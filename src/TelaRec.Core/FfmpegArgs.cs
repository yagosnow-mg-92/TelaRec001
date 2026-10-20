namespace TelaRec.Core;

/// <summary>Monta as linhas de comando do FFmpeg. Lógica pura, coberta por testes unitários.</summary>
public static class FfmpegArgs
{
    // MP4 fragmentado: o arquivo continua reproduzível mesmo se o processo for interrompido.
    private const string FragFlags = "-movflags +frag_keyframe+empty_moov+default_base_moof";
    private const string ColorTags = "-colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv";
    // Dimensões pares (exigência do H.264 4:2:0), conversão BT.709 e sem qualquer redimensionamento.
    private const string Tail = "crop=trunc(iw/2)*2:trunc(ih/2)*2,scale=out_color_matrix=bt709:out_range=tv,format=yuv420p";

    public static int QualityValue(VideoQuality q, VideoEncoder e)
    {
        if (e == VideoEncoder.Cpu)
            return q switch { VideoQuality.Economy => 29, VideoQuality.High => 22, _ => 26 };
        return q switch { VideoQuality.Economy => 31, VideoQuality.High => 24, _ => 28 };
    }

    public static string EncoderArgs(VideoEncoder e, VideoQuality q)
    {
        int v = QualityValue(q, e);
        return e switch
        {
            VideoEncoder.Nvenc => $"-c:v h264_nvenc -preset p4 -rc vbr -cq {v} -b:v 0 -profile:v high",
            VideoEncoder.Qsv => $"-c:v h264_qsv -preset medium -global_quality {v} -profile:v high",
            VideoEncoder.Amf => $"-c:v h264_amf -quality balanced -rc cqp -qp_i {v} -qp_p {v + 2} -profile:v high",
            _ => $"-c:v libx264 -preset veryfast -crf {v} -profile:v high",
        };
    }

    public static string BuildCapture(MonitorInfo m, CaptureMode mode, int fps, VideoQuality q, VideoEncoder e, string output)
    {
        string common = $"{EncoderArgs(e, q)} -g {fps * 2} {ColorTags} -fps_mode cfr -r {fps} {FragFlags} -f mp4 {Quote(output)}";

        if (mode == CaptureMode.Dxgi)
        {
            if (m.AdapterIndex < 0 || m.OutputIndex < 0)
                throw new InvalidOperationException("Monitor sem mapeamento DXGI.");
            return $"-hide_banner -loglevel error -y -init_hw_device d3d11va=hw:{m.AdapterIndex} -filter_hw_device hw " +
                   $"-filter_complex \"ddagrab=output_idx={m.OutputIndex}:framerate={fps}:draw_mouse=1,hwdownload,format=bgra,{Tail}\" {common}";
        }

        return $"-hide_banner -loglevel error -y -f gdigrab -framerate {fps} -offset_x {m.X} -offset_y {m.Y} " +
               $"-video_size {m.Width}x{m.Height} -draw_mouse 1 -i desktop -vf \"{Tail}\" {common}";
    }

    public static string BuildRemux(string input, string output) =>
        $"-hide_banner -loglevel error -y -i {Quote(input)} -c copy -movflags +faststart -f mp4 {Quote(output)}";

    public static string BuildConcat(string listFile, string output) =>
        $"-hide_banner -loglevel error -y -f concat -safe 0 -i {Quote(listFile)} -c copy -movflags +faststart -f mp4 {Quote(output)}";

    public static string BuildEncoderProbe(VideoEncoder e) =>
        $"-hide_banner -loglevel error -f lavfi -i color=c=black:s=1280x720:r=30 -frames:v 15 -pix_fmt yuv420p " +
        $"{EncoderArgs(e, VideoQuality.Balanced)} -f null -";

    public static string ConcatLine(string path) =>
        "file '" + path.Replace('\\', '/').Replace("'", "'\\''") + "'";

    public static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";
}
