using System.Collections.Concurrent;
using BLGNTube.Web.Data;
using BLGNTube.Web.Models;
using Microsoft.Extensions.Options;

namespace BLGNTube.Web.Services;

public class DownloadJobManager
{
    private readonly ConcurrentDictionary<string, DownloadJob> _jobs = new();
    private readonly YtDlpService _ytDlp;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DownloaderOptions _options;
    private readonly ILogger<DownloadJobManager> _logger;

    public DownloadJobManager(
        YtDlpService ytDlp,
        IServiceScopeFactory scopeFactory,
        IOptions<DownloaderOptions> options,
        ILogger<DownloadJobManager> logger)
    {
        _ytDlp = ytDlp;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public DownloadJob? Get(string id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public DownloadJob Start(string url, MediaFormat format, string? quality,
        string? userId, string ipAddress, MediaInfo? info)
    {
        var job = new DownloadJob
        {
            SourceUrl = url,
            Format = format,
            Quality = quality,
            UserId = userId,
            IpAddress = ipAddress,
            Title = info?.Title ?? "Medya",
            ThumbnailUrl = info?.ThumbnailUrl,
            SiteName = info?.SiteName,
            State = DownloadJobState.Queued
        };

        _jobs[job.Id] = job;
        _ = Task.Run(() => RunAsync(job));
        return job;
    }

    private async Task RunAsync(DownloadJob job)
    {
        var jobDir = Path.Combine(ResolveOutputRoot(), job.Id);
        try
        {
            job.State = DownloadJobState.Downloading;

            await _ytDlp.ExecuteAsync(job, jobDir, progress: pct =>
            {
                job.Progress = pct;
                if (pct >= 99.9) job.State = DownloadJobState.Processing;
            });

            job.Progress = 100;
            job.State = DownloadJobState.Completed;

            await PersistRecordAsync(job);
            ScheduleCleanup(job, jobDir);
        }
        catch (Exception ex)
        {
            job.State = DownloadJobState.Failed;
            job.Error = ex.Message;
            _logger.LogError(ex, "İndirme job'u başarısız: {Url}", job.SourceUrl);
            TryDeleteDir(jobDir);
        }
    }

    private async Task PersistRecordAsync(DownloadJob job)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.DownloadRecords.Add(new DownloadRecord
            {
                UserId = job.UserId,
                IpAddress = job.IpAddress,
                SourceUrl = job.SourceUrl,
                Title = job.Title,
                ThumbnailUrl = job.ThumbnailUrl,
                SiteName = job.SiteName,
                Format = job.Format,
                Quality = job.Quality,
                FileName = job.FileName,
                FileSizeBytes = job.FileSizeBytes,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İndirme kaydı veritabanına yazılamadı.");
        }
    }

    private void ScheduleCleanup(DownloadJob job, string jobDir)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(30));
            TryDeleteDir(jobDir);
            _jobs.TryRemove(job.Id, out _);
        });
    }

    private void TryDeleteDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
        catch (Exception ex) { _logger.LogWarning(ex, "Geçici klasör silinemedi: {Dir}", dir); }
    }

    private string ResolveOutputRoot()
    {
        var dir = _options.OutputDirectory;
        if (!Path.IsPathRooted(dir))
            dir = Path.Combine(AppContext.BaseDirectory, dir);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
