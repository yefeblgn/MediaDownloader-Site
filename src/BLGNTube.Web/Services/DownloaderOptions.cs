namespace BLGNTube.Web.Services;

public class DownloaderOptions
{
    public const string SectionName = "Downloader";

    public string YtDlpPath { get; set; } = "yt-dlp";

    public string FfmpegPath { get; set; } = "ffmpeg";

    public string OutputDirectory { get; set; } = "wwwroot/downloads";

    public int TimeoutSeconds { get; set; } = 600;

    public int MaxDurationSeconds { get; set; } = 0;
}
