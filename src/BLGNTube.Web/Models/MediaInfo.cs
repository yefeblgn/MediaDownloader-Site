namespace BLGNTube.Web.Models;

public class MediaInfo
{
    public string Title { get; set; } = string.Empty;
    public string? Uploader { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? SiteName { get; set; }

    public double? DurationSeconds { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public List<VideoQualityOption> VideoQualities { get; set; } = new();

    public string DurationLabel
    {
        get
        {
            if (DurationSeconds is null || DurationSeconds <= 0) return "—";
            var ts = TimeSpan.FromSeconds(DurationSeconds.Value);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
}

public class VideoQualityOption
{
    public int Height { get; set; }

    public string Label { get; set; } = string.Empty;
}
