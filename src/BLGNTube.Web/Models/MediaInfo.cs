namespace BLGNTube.Web.Models;

/// <summary>
/// yt-dlp ile bir URL incelendiğinde elde edilen medya önizleme bilgisi.
/// Kullanıcıya indirmeden önce ne alacağını göstermek için kullanılır.
/// </summary>
public class MediaInfo
{
    public string Title { get; set; } = string.Empty;
    public string? Uploader { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? SiteName { get; set; }

    /// <summary>Süre (saniye). Bilinmiyorsa null.</summary>
    public double? DurationSeconds { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>İndirilebilir video kalite seçenekleri (yüksekten düşüğe).</summary>
    public List<VideoQualityOption> VideoQualities { get; set; } = new();

    /// <summary>İnsan-okunur süre etiketi (ör. "3:45").</summary>
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

/// <summary>Seçilebilir bir video kalitesi (ör. 1080p).</summary>
public class VideoQualityOption
{
    /// <summary>Dikey çözünürlük (ör. 1080). Sıralama ve seçim anahtarı.</summary>
    public int Height { get; set; }

    /// <summary>Kullanıcıya gösterilen etiket (ör. "1080p").</summary>
    public string Label { get; set; } = string.Empty;
}
