namespace BLGNTube.Web.Services;

/// <summary>
/// İndirme motorunun yapılandırması (appsettings.json -> "Downloader").
/// yt-dlp ve ffmpeg yolları ile çıktı/limit ayarlarını içerir.
/// </summary>
public class DownloaderOptions
{
    public const string SectionName = "Downloader";

    /// <summary>yt-dlp çalıştırılabilir yolu. PATH'te ise sadece "yt-dlp" yeterli.</summary>
    public string YtDlpPath { get; set; } = "yt-dlp";

    /// <summary>ffmpeg çalıştırılabilir yolu/dizini. Boşsa PATH kullanılır.</summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>İndirilen dosyaların geçici olarak tutulduğu kök dizin (wwwroot'a göreceli değil; mutlak ya da içerik köküne göreceli).</summary>
    public string OutputDirectory { get; set; } = "wwwroot/downloads";

    /// <summary>Medya bilgisi/indirme için saniye cinsinden zaman aşımı.</summary>
    public int TimeoutSeconds { get; set; } = 600;

    /// <summary>Tek seferde izin verilen en uzun medya süresi (saniye). 0 = sınırsız.</summary>
    public int MaxDurationSeconds { get; set; } = 0;
}
