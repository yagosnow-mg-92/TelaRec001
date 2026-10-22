using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

namespace TelaRec.Services;

internal sealed record DriveUploadResult(string Url, string AccountEmail);

/// <summary>OAuth para aplicativo desktop e upload retomável direto do MP4 para uma pasta do Drive.</summary>
internal sealed class GoogleDriveUploader
{
    private static readonly string[] Scopes = { DriveService.Scope.Drive };
    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TelaRec", "google-tokens");

    public async Task<DriveUploadResult> UploadAsync(string clientId, string clientSecret, string folderUrl, string videoPath,
        IProgress<(long Sent, long Total)>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new InvalidOperationException("Informe o ID do cliente OAuth do aplicativo.");
        var folderId = ParseFolderId(folderUrl);
        var credential = await AuthorizeAsync(clientId, clientSecret, cancellationToken);
        var drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "TelaRec"
        });

        // A consulta também valida acesso e se a URL aponta para uma pasta.
        var folder = await drive.Files.Get(folderId).SetFields("mimeType").SetSupportsAllDrives(true).ExecuteAsync(cancellationToken);
        if (folder.MimeType != "application/vnd.google-apps.folder")
            throw new InvalidOperationException("O link informado não aponta para uma pasta do Google Drive.");

        var account = await drive.About.Get().SetFields("user(emailAddress)").ExecuteAsync(cancellationToken);
        var metadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = Path.GetFileName(videoPath),
            MimeType = "video/mp4",
            Parents = new[] { folderId }
        };
        await using var stream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var request = drive.Files.Create(metadata, stream, "video/mp4");
        request.Fields = "id,name,webViewLink";
        request.SupportsAllDrives = true;
        request.ChunkSize = ResumableUpload.MinimumChunkSize * 4;
        request.ProgressChanged += p => progress?.Report((Math.Max(0, p.BytesSent), stream.Length));
        var result = await request.UploadAsync(cancellationToken);
        if (result.Status != UploadStatus.Completed)
            throw result.Exception ?? new IOException("O Google Drive não concluiu o envio.");
        var uploaded = request.ResponseBody;
        if (uploaded == null || string.IsNullOrWhiteSpace(uploaded.Id))
            throw new IOException("O Drive concluiu o envio, mas não retornou o identificador do vídeo.");
        var url = uploaded.WebViewLink ?? $"https://drive.google.com/file/d/{uploaded.Id}/view";
        return new DriveUploadResult(url, account.User?.EmailAddress ?? "");
    }

    public async Task<string> ValidateFolderAsync(string clientId, string clientSecret, string folderUrl, CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(clientId, clientSecret, cancellationToken);
        var drive = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential, ApplicationName = "TelaRec" });
        var folder = await drive.Files.Get(ParseFolderId(folderUrl)).SetFields("mimeType,capabilities(canAddChildren)")
            .SetSupportsAllDrives(true).ExecuteAsync(cancellationToken);
        if (folder.MimeType != "application/vnd.google-apps.folder") throw new InvalidOperationException("O link informado não aponta para uma pasta do Google Drive.");
        if (folder.Capabilities?.CanAddChildren != true) throw new InvalidOperationException("A conta selecionada pode acessar a pasta, mas não tem permissão para adicionar arquivos nela.");
        var account = await drive.About.Get().SetFields("user(emailAddress)").ExecuteAsync(cancellationToken);
        return account.User?.EmailAddress ?? "Conta Google autorizada";
    }

    private static async Task<UserCredential> AuthorizeAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = Scopes,
            AccessType = "offline",
            Prompt = "select_account"
        };
        return await GoogleWebAuthorizationBroker.AuthorizeAsync(initializer, Scopes, "user", ct,
            new ProtectedTokenDataStore(TokenPath));
    }

    public static void ClearSavedAuthorization() => new ProtectedTokenDataStore(TokenPath).ClearAsync().GetAwaiter().GetResult();

    internal static string ParseFolderId(string url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Host != "drive.google.com" && uri.Host != "www.drive.google.com"))
            throw new InvalidOperationException("Cole um link válido de uma pasta do Google Drive.");
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.FindIndex(parts, p => p.Equals("folders", StringComparison.OrdinalIgnoreCase));
        var id = index >= 0 && index + 1 < parts.Length ? parts[index + 1] :
            System.Web.HttpUtility.ParseQueryString(uri.Query)["id"];
        if (string.IsNullOrWhiteSpace(id) || id.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_')))
            throw new InvalidOperationException("Não encontrei o ID da pasta nesse link. Copie o link de compartilhamento da pasta.");
        return id;
    }
}

/// <summary>Guarda refresh tokens protegidos com DPAPI, vinculados à conta Windows atual.</summary>
internal sealed class ProtectedTokenDataStore(string directory) : IDataStore
{
    public Task StoreAsync<T>(string key, T value)
    {
        Directory.CreateDirectory(directory);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllText(GetPath(key), Convert.ToBase64String(encrypted));
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path)) return Task.FromResult<T?>(default);
        var encrypted = Convert.FromBase64String(File.ReadAllText(path));
        var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Task.FromResult(JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes)));
    }

    public Task DeleteAsync<T>(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task ClearAsync() { if (Directory.Exists(directory)) Directory.Delete(directory, true); return Task.CompletedTask; }
    private string GetPath(string key) => Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".json");
}
