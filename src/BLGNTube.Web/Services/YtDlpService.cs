using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using BLGNTube.Web.Models;
using Microsoft.Extensions.Options;

namespace BLGNTube.Web.Services;

/// <summary>
/// yt-dlp (ve ffmpeg) komut satırı aracını saran servis. Bir URL'in medya
/// bilgisini çeker ve gerçek indirme/dönüştürme işlemini yürütür.
/// metube gibi açık kaynak projelerle aynı motoru (yt-dlp) kullanır.
/// </summary>
public class YtDlpService
{
    private readonly DownloaderOptions _options;
    private readonly ILogger<YtDlpService> _logger;

    public YtDlpService(IOptions<DownloaderOptions> options, ILogger<YtDlpService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// URL'i yt-dlp ile inceler ve başlık, küçük resim, süre ve kalite
    /// seçeneklerini içeren bir önizleme bilgisi döndürür.
    /// </summary>
    public async Task<MediaInfo> GetMediaInfoAsync(string url, CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "-J",                 // tek JSON çıktısı
            "--no-playlist",      // tek video
            "--no-warnings",
            "--no-progress",
            url
        };

        var result = await RunAsync(_options.YtDlpPath, args, onLine: null, ct: ct);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
        {
            _logger.LogWarning("yt-dlp info başarısız: {Err}", result.StdErr);
            throw new InvalidOperationException(FriendlyError(result.StdErr));
        }

        using var doc = JsonDocument.Parse(result.StdOut);
        var root = doc.RootElement;

        // Playlist/çoklu girdi gelirse ilk girdiyi al.
        if (root.TryGetProperty("entries", out var entries) &&
            entries.ValueKind == JsonValueKind.Array && entries.GetArrayLength() > 0)
        {
            root = entries[0];
        }

        var info = new MediaInfo
        {
            OriginalUrl = url,
            Title = GetString(root, "title") ?? "Başlıksız medya",
            Uploader = GetString(root, "uploader") ?? GetString(root, "channel"),
            ThumbnailUrl = GetString(root, "thumbnail"),
            SiteName = GetString(root, "extractor_key") ?? GetString(root, "extractor"),
            DurationSeconds = root.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number
                ? d.GetDouble()
                : null
        };

        info.VideoQualities = ExtractQualities(root);
        return info;
    }

    /// <summary>Mevcut video formatlarından benzersiz çözünürlük seçenekleri çıkarır.</summary>
    private static List<VideoQualityOption> ExtractQualities(JsonElement root)
    {
        var heights = new SortedSet<int>();
        if (root.TryGetProperty("formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in formats.EnumerateArray())
            {
                // Sadece görüntü içeren formatları dikkate al.
                if (f.TryGetProperty("vcodec", out var vc) && vc.GetString() == "none") continue;
                if (f.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number)
                {
                    var height = h.GetInt32();
                    if (height > 0) heights.Add(height);
                }
            }
        }

        // En yaygın hedef kalitelere sınırla; mevcut olanların altına yuvarla.
        var standard = new[] { 2160, 1440, 1080, 720, 480, 360, 240, 144 };
        var available = standard.Where(s => heights.Any(h => h >= s)).ToList();

        var options = available
            .Select(s => new VideoQualityOption
            {
                Height = s,
                Label = s >= 2160 ? "4K (2160p)" : $"{s}p"
            })
            .ToList();

        // Hiç format okunamadıysa makul varsayılanlar sun.
        if (options.Count == 0)
        {
            options = new List<VideoQualityOption>
            {
                new() { Height = 1080, Label = "1080p" },
                new() { Height = 720, Label = "720p" },
                new() { Height = 480, Label = "480p" }
            };
        }

        return options;
    }

    /// <summary>
    /// Bir indirme job'unu yürütür: yt-dlp'yi uygun formatla çalıştırır,
    /// ilerlemeyi <paramref name="progress"/> ile bildirir ve tamamlandığında
    /// job üzerindeki dosya alanlarını doldurur.
    /// </summary>
    public async Task ExecuteAsync(DownloadJob job, string outputDir, Action<double>? progress, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);

        // Her job kendi alt klasörüne indirir; böylece sonuç dosyasını
        // güvenle tespit edebiliriz.
        var outputTemplate = Path.Combine(outputDir, "%(title).80B [%(id)s].%(ext)s");

        var args = new List<string>
        {
            "--no-playlist",
            "--no-warnings",
            "--newline",
            "--progress-template", "download:__PROG__ %(progress._percent_str)s",
            "--no-mtime",
            "-o", outputTemplate
        };

        // --ffmpeg-location yalnızca gerçek bir yol/dizin verildiğinde geçilir.
        // Sadece "ffmpeg" gibi çıplak bir komut adıysa yt-dlp'nin onu PATH'ten
        // bulmasına izin veririz (aksi halde yt-dlp onu yol sanıp bulamaz).
        var ffmpeg = _options.FfmpegPath;
        if (!string.IsNullOrWhiteSpace(ffmpeg) &&
            (ffmpeg.Contains('/') || ffmpeg.Contains('\\') || File.Exists(ffmpeg) || Directory.Exists(ffmpeg)))
        {
            args.Add("--ffmpeg-location");
            args.Add(ffmpeg);
        }

        if (job.Format == MediaFormat.Mp3)
        {
            // En iyi sesi çıkar ve 320kbps MP3'e dönüştür.
            args.AddRange(new[]
            {
                "-x",
                "--audio-format", "mp3",
                "--audio-quality", "0"
            });
        }
        else
        {
            // İstenen yüksekliğe kadar en iyi video+ses, MP4'te birleştir.
            var height = ParseHeight(job.Quality);
            var format = height > 0
                ? $"bv*[height<={height}]+ba/b[height<={height}]/bv*+ba/b"
                : "bv*+ba/b";
            args.AddRange(new[]
            {
                "-f", format,
                "--merge-output-format", "mp4"
            });
        }

        args.Add(job.SourceUrl);

        var result = await RunAsync(_options.YtDlpPath, args, onLine: line =>
        {
            var pct = TryParseProgress(line);
            if (pct is not null) progress?.Invoke(pct.Value);
        }, ct: ct);

        if (result.ExitCode != 0)
        {
            _logger.LogWarning("yt-dlp indirme başarısız: {Err}", result.StdErr);
            throw new InvalidOperationException(FriendlyError(result.StdErr));
        }

        // İndirilen dosyayı tespit et (alt klasördeki en büyük/yeni dosya).
        var file = new DirectoryInfo(outputDir)
            .GetFiles()
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ThenByDescending(f => f.Length)
            .FirstOrDefault();

        if (file is null)
            throw new InvalidOperationException("İndirme tamamlandı ancak çıktı dosyası bulunamadı.");

        job.FilePath = file.FullName;
        job.FileName = file.Name;
        job.FileSizeBytes = file.Length;
    }

    private static int ParseHeight(string? quality)
    {
        if (string.IsNullOrWhiteSpace(quality)) return 0;
        var digits = new string(quality.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var h) ? h : 0;
    }

    /// <summary>"download:__PROG__  45.3%" biçimli satırdan yüzdeyi okur.</summary>
    private static double? TryParseProgress(string line)
    {
        const string marker = "__PROG__";
        var idx = line.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;

        var rest = line[(idx + marker.Length)..].Trim().TrimEnd('%').Trim();
        return double.TryParse(rest, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct)
            ? Math.Clamp(pct, 0, 100)
            : null;
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>yt-dlp stderr çıktısını kullanıcıya gösterilebilir kısa mesaja çevirir.</summary>
    private static string FriendlyError(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return "Medya işlenemedi. Bağlantıyı kontrol edin.";

        var lower = stderr.ToLowerInvariant();
        if (lower.Contains("unsupported url"))
            return "Bu site/bağlantı desteklenmiyor.";
        if (lower.Contains("private") || lower.Contains("login") || lower.Contains("sign in"))
            return "Bu içerik gizli ya da giriş gerektiriyor, indirilemez.";
        if (lower.Contains("not found") || lower.Contains("404") || lower.Contains("unavailable"))
            return "İçerik bulunamadı veya kaldırılmış.";
        if (lower.Contains("no such file") || lower.Contains("not recognized") || lower.Contains("cannot find"))
            return "yt-dlp veya ffmpeg bulunamadı. Sunucu kurulumunu kontrol edin.";

        // Son anlamlı satırı döndür.
        var lastLine = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                             .LastOrDefault();
        return lastLine ?? "Medya işlenemedi.";
    }

    private record ProcessResult(int ExitCode, string StdOut, string StdErr);

    /// <summary>Bir komutu çalıştırır, çıktıyı toplar ve satır geri çağrısı sağlar.</summary>
    private async Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> args,
        Action<string>? onLine, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stdout.AppendLine(e.Data);
            onLine?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            stderr.AppendLine(e.Data);
            // yt-dlp ilerlemeyi bazen stderr'e de yazabilir.
            onLine?.Invoke(e.Data);
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"'{fileName}' başlatılamadı. yt-dlp kurulu ve PATH'te mi? ({ex.Message})", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { /* yok say */ }
            throw new InvalidOperationException("İşlem zaman aşımına uğradı veya iptal edildi.");
        }

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
