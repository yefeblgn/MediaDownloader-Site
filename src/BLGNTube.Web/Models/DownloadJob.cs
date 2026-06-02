namespace BLGNTube.Web.Models;

/// <summary>Bir indirme job'unun yaşam döngüsü durumu.</summary>
public enum DownloadJobState
{
    Queued = 0,
    Downloading = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

/// <summary>
/// Arka planda çalışan tek bir indirme işini temsil eder. Bellekte
/// (DownloadJobManager içinde) tutulur ve istemci durumu polling ile takip eder.
/// </summary>
public class DownloadJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DownloadJobState State { get; set; } = DownloadJobState.Queued;

    /// <summary>0-100 arası ilerleme yüzdesi.</summary>
    public double Progress { get; set; }

    public string SourceUrl { get; set; } = string.Empty;
    public MediaFormat Format { get; set; }
    public string? Quality { get; set; }

    public string Title { get; set; } = "Medya";
    public string? ThumbnailUrl { get; set; }
    public string? SiteName { get; set; }

    /// <summary>İndirme tamamlandığında sunucudaki dosya yolu.</summary>
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }

    /// <summary>Hata durumunda kullanıcıya gösterilecek mesaj.</summary>
    public string? Error { get; set; }

    /// <summary>İndirmeyi başlatan üye (varsa).</summary>
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>İstemciye gönderilen, dosya yolu gibi hassas alanları içermeyen özet.</summary>
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
