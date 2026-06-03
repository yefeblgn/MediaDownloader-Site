namespace BLGNTube.Web.Models;

public enum DownloadJobState
{
    Queued = 0,
    Downloading = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public class DownloadJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DownloadJobState State { get; set; } = DownloadJobState.Queued;

    public double Progress { get; set; }

    public string SourceUrl { get; set; } = string.Empty;
    public MediaFormat Format { get; set; }
    public string? Quality { get; set; }

    public string Title { get; set; } = "Medya";
    public string? ThumbnailUrl { get; set; }
    public string? SiteName { get; set; }

    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }

    public string? Error { get; set; }

    public string? UserId { get; set; }
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public object ToStatusDto() => new
    {
        id = Id,
        state = State.ToString(),
        progress = Math.Round(Progress, 1),
        title = Title,
        thumbnailUrl = ThumbnailUrl,
        siteName = SiteName,
        fileName = FileName,
        fileSizeBytes = FileSizeBytes,
        error = Error,
        ready = State == DownloadJobState.Completed
    };
}
