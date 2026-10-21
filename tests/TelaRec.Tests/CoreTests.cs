using TelaRec.Core;

namespace TelaRec.Tests;

public class FfmpegArgsTests
{
    private static MonitorInfo Mon(int x, int y, int w, int h, int a = 0, int o = 0) =>
        new(2, @"\\.\DISPLAY2", x, y, w, h, false, 100, a, o);

    [Fact]
    public void Dxgi_UsesAdapterAndOutputIndex()
    {
        var args = FfmpegArgs.BuildCapture(Mon(-1920, 0, 1920, 1080, 1, 2), CaptureMode.Dxgi, 30,
            VideoQuality.Balanced, VideoEncoder.Cpu, @"C:\v\a.mp4");
        Assert.Contains("d3d11va=hw:1", args);
        Assert.Contains("output_idx=2", args);
        Assert.Contains("framerate=30", args);
        Assert.Contains("libx264", args);
    }

    [Fact]
    public void Dxgi_WithoutMapping_Throws() =>
        Assert.Throws<InvalidOperationException>(() => FfmpegArgs.BuildCapture(
            Mon(0, 0, 1920, 1080, -1, -1), CaptureMode.Dxgi, 30, VideoQuality.Balanced, VideoEncoder.Cpu, "a.mp4"));

    [Fact]
    public void Gdi_SupportsNegativeCoordinates()
    {
        var args = FfmpegArgs.BuildCapture(Mon(-2560, -300, 2560, 1440), CaptureMode.Gdi, 60,
            VideoQuality.High, VideoEncoder.Cpu, "a.mp4");
        Assert.Contains("-offset_x -2560", args);
        Assert.Contains("-offset_y -300", args);
        Assert.Contains("-video_size 2560x1440", args);
    }

    [Fact]
    public void Output_NeverScalesResolution()
    {
        var args = FfmpegArgs.BuildCapture(Mon(0, 0, 1921, 1081), CaptureMode.Gdi, 30,
            VideoQuality.Balanced, VideoEncoder.Cpu, "a.mp4");
        Assert.Contains("crop=trunc(iw/2)*2:trunc(ih/2)*2", args); // só ajusta para número par
        Assert.DoesNotContain("scale=w=", args);
        Assert.DoesNotContain("scale=1", args);
    }

    [Theory]
    [InlineData(VideoEncoder.Nvenc, "h264_nvenc")]
    [InlineData(VideoEncoder.Qsv, "h264_qsv")]
    [InlineData(VideoEncoder.Amf, "h264_amf")]
    [InlineData(VideoEncoder.Cpu, "libx264")]
    public void EncoderArgs_SelectsCodec(VideoEncoder e, string expected) =>
        Assert.Contains(expected, FfmpegArgs.EncoderArgs(e, VideoQuality.Balanced));

    [Fact]
    public void HigherQuality_MeansLowerQuantizer()
    {
        Assert.True(FfmpegArgs.QualityValue(VideoQuality.High, VideoEncoder.Cpu) <
                    FfmpegArgs.QualityValue(VideoQuality.Economy, VideoEncoder.Cpu));
    }

    [Fact]
    public void ConcatLine_EscapesBackslashAndQuote() =>
        Assert.Equal("file 'C:/a/it'\\''s/seg001.mp4'", FfmpegArgs.ConcatLine(@"C:\a\it's\seg001.mp4"));

    [Fact]
    public void Capture_IsFragmentedMp4() =>
        Assert.Contains("frag_keyframe+empty_moov", FfmpegArgs.BuildCapture(
            Mon(0, 0, 1920, 1080), CaptureMode.Gdi, 30, VideoQuality.Balanced, VideoEncoder.Cpu, "a.mp4"));
}

public class HelpersTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "telarec-test-" + Guid.NewGuid().ToString("N"));
    public HelpersTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    [Fact]
    public void UniquePath_DoesNotOverwrite()
    {
        File.WriteAllText(Path.Combine(_dir, "video.mp4"), "x");
        var p = FileNamer.UniquePath(_dir, "video");
        Assert.EndsWith("video (2).mp4", p);
        File.WriteAllText(p, "x");
        Assert.EndsWith("video (3).mp4", FileNamer.UniquePath(_dir, "video"));
    }

    [Theory]
    [InlineData("meu:video?.mp4", "meuvideo")]
    [InlineData("  aula 1  ", "aula 1")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Sanitize_RemovesInvalidChars(string? input, string expected) =>
        Assert.Equal(expected, FileNamer.Sanitize(input));

    [Fact]
    public void TimeFormat_HandlesLongRecordings() =>
        Assert.Equal("100:00:05", TimeFormat.Format(TimeSpan.FromSeconds(360005)));

    [Fact]
    public void Settings_RoundTrip()
    {
        var path = Path.Combine(_dir, "s.json");
        new AppSettings { Fps = 60, Quality = VideoQuality.High, FileName = "x", ClickEffect = true, ClickSound = false }.Save(path);
        var s = AppSettings.Load(path);
        Assert.Equal(60, s.Fps);
        Assert.Equal(VideoQuality.High, s.Quality);
        Assert.Equal("x", s.FileName);
        Assert.True(s.ClickEffect);
        Assert.False(s.ClickSound);
    }

    [Fact]
    public void Settings_CorruptedFile_FallsBackToDefaults()
    {
        var path = Path.Combine(_dir, "bad.json");
        File.WriteAllText(path, "{{{ nope");
        Assert.Equal(30, AppSettings.Load(path).Fps);
    }

    [Fact]
    public void MonitorInfo_GeometryComparison()
    {
        var a = new MonitorInfo(1, "D1", -1920, 0, 1920, 1080, false, 100);
        Assert.True(a.SameGeometryAs(a with { AdapterIndex = 3 }));
        Assert.False(a.SameGeometryAs(a with { Width = 1280 }));
    }
}
