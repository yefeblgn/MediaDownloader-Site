using System.Security.Claims;
using BLGNTube.Web.Models;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace BLGNTube.Web.Controllers;

[ApiController]
[Route("api/download")]
public class DownloadController : ControllerBase
{
    private readonly YtDlpService _ytDlp;
    private readonly DownloadJobManager _jobs;
    private readonly QuotaService _quota;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(
        YtDlpService ytDlp,
        DownloadJobManager jobs,
        QuotaService quota,
        ILogger<DownloadController> logger)
    {
        _ytDlp = ytDlp;
        _jobs = jobs;
        _quota = quota;
        _logger = logger;
    }

    public record InfoRequest(string Url);
    public record StartRequest(string Url, string Format, string? Quality);

    [HttpPost("info")]
    public async Task<IActionResult> Info([FromBody] InfoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url) || !IsValidUrl(req.Url))
            return BadRequest(new { error = "Geçerli bir bağlantı (URL) girin." });

        try
        {
            var info = await _ytDlp.GetMediaInfoAsync(req.Url.Trim());
            return Ok(new
            {
                title = info.Title,
                uploader = info.Uploader,
                thumbnailUrl = info.ThumbnailUrl,
                siteName = info.SiteName,
                duration = info.DurationLabel,
                qualities = info.VideoQualities.Select(q => new { height = q.Height, label = q.Label })
            });
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url) || !IsValidUrl(req.Url))
            return BadRequest(new { error = "Geçerli bir bağlantı (URL) girin." });

        var userId = GetUserId();
        var ip = GetClientIp();

        var quota = await _quota.GetAsync(userId, ip);
        if (!quota.CanDownload)
        {
            var msg = userId is null
                ? "Günlük 10 indirme hakkınız doldu. Daha fazlası için ücretsiz hesap oluşturun (günde 100 indirme)."
                : "Günlük 100 indirme hakkınız doldu. Yarın tekrar deneyin.";
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = msg, remaining = 0, limit = quota.Limit });
        }

        var format = string.Equals(req.Format, "mp3", StringComparison.OrdinalIgnoreCase)
            ? MediaFormat.Mp3 : MediaFormat.Mp4;

        MediaInfo? info = null;
        try { info = await _ytDlp.GetMediaInfoAsync(req.Url.Trim()); }
        catch (Exception ex) { _logger.LogInformation("Bilgi alınamadı, indirme yine de denenecek: {Msg}", ex.Message); }

        var job = _jobs.Start(req.Url.Trim(), format, req.Quality, userId, ip, info);

        return Ok(new
        {
            jobId = job.Id,
            status = job.ToStatusDto(),
            remaining = quota.Remaining - 1
        });
    }

    [HttpGet("status/{id}")]
    public IActionResult Status(string id)
    {
        var job = _jobs.Get(id);
        if (job is null) return NotFound(new { error = "Job bulunamadı veya süresi doldu." });
        return Ok(job.ToStatusDto());
    }

    [HttpGet("file/{id}")]
    public IActionResult File(string id)
    {
        var job = _jobs.Get(id);
        if (job is null || job.State != DownloadJobState.Completed || job.FilePath is null || !System.IO.File.Exists(job.FilePath))
            return NotFound(new { error = "Dosya hazır değil veya süresi dolmuş." });

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(job.FilePath, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(job.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var downloadName = job.FileName ?? Path.GetFileName(job.FilePath);
        return File(stream, contentType, downloadName);
    }

    private static bool IsValidUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private string? GetUserId() =>
        User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private string GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
