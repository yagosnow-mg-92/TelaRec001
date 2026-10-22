using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TelaRec;

/// <summary>Janela de progresso do upload e apresentação do link final.</summary>
internal sealed class DriveTransferWindow : Window
{
    private readonly TextBlock _status = new() { Text = "Preparando envio…", Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar _bar = new() { Height = 18, Minimum = 0, Maximum = 100 };
    private readonly TextBox _url = new() { IsReadOnly = true, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 14, 0, 0), Padding = new Thickness(8) };
    private readonly Button _copy = new() { Content = "Copiar link", IsEnabled = false, Margin = new Thickness(0, 12, 8, 0), Padding = new Thickness(16, 8, 16, 8) };
    private readonly Button _close = new() { Content = "Fechar", IsEnabled = false, Margin = new Thickness(0, 12, 0, 0), Padding = new Thickness(16, 8, 16, 8) };
    private bool _finished;

    public DriveTransferWindow()
    {
        Title = "Envio para o Google Drive"; Width = 520; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(23, 26, 33)); Foreground = new SolidColorBrush(Color.FromRgb(232, 234, 238));
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(_copy); buttons.Children.Add(_close);
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(_status); content.Children.Add(_bar); content.Children.Add(_url); content.Children.Add(buttons);
        Content = content;
        _copy.Click += (_, _) => { try { Clipboard.SetText(_url.Text); _status.Text = "Link copiado para a área de transferência."; } catch { _status.Text = "Não foi possível copiar o link."; } };
        _close.Click += (_, _) => Close();
        Closing += (_, e) => { if (!_finished) e.Cancel = true; };
    }

    public void Report(long sent, long total)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(() => Report(sent, total))); return; }
        if (_finished) return;
        _bar.IsIndeterminate = total <= 0;
        if (total > 0)
        {
            _bar.Value = Math.Clamp(sent * 100d / total, 0, 100);
            _status.Text = $"Enviando vídeo… {FormatBytes(sent)} de {FormatBytes(total)}";
        }
    }

    public void Complete(string url)
    {
        _finished = true;
        _bar.IsIndeterminate = false; _bar.Value = 100;
        _status.Text = "Upload concluído. Copie o link para compartilhar no DevOps.";
        _url.Text = url; _url.Visibility = Visibility.Visible;
        _copy.IsEnabled = true; _close.IsEnabled = true;
    }

    public void Fail(string message)
    {
        _finished = true;
        _status.Foreground = new SolidColorBrush(Color.FromRgb(240, 69, 79));
        _status.Text = "O upload não foi concluído: " + message;
        _bar.IsIndeterminate = false; _close.IsEnabled = true;
    }

    private static string FormatBytes(long value)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" }; double size = value; int i = 0;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.#} {units[i]}";
    }
}
