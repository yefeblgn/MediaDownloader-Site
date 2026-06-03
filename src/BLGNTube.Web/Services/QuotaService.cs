using BLGNTube.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BLGNTube.Web.Services;

public record QuotaStatus(int Used, int Limit)
{
    public int Remaining => Math.Max(0, Limit - Used);
    public bool CanDownload => Used < Limit;
}

public class QuotaService
{
    public const int AnonymousDailyLimit = 10;
    public const int AuthenticatedDailyLimit = 100;

    private readonly ApplicationDbContext _db;

    public QuotaService(ApplicationDbContext db) => _db = db;

    public static int LimitFor(bool authenticated) =>
        authenticated ? AuthenticatedDailyLimit : AnonymousDailyLimit;

    private static DateTime TodayStartUtc => DateTime.UtcNow.Date;

    public async Task<QuotaStatus> GetForUserAsync(string userId)
    {
        var used = await _db.DownloadRecords
            .CountAsync(r => r.UserId == userId && r.CreatedAt >= TodayStartUtc);
        return new QuotaStatus(used, AuthenticatedDailyLimit);
    }

    public async Task<QuotaStatus> GetForIpAsync(string ipAddress)
    {
        var used = await _db.DownloadRecords
            .CountAsync(r => r.UserId == null && r.IpAddress == ipAddress && r.CreatedAt >= TodayStartUtc);
        return new QuotaStatus(used, AnonymousDailyLimit);
    }

    public Task<QuotaStatus> GetAsync(string? userId, string ipAddress) =>
        string.IsNullOrEmpty(userId) ? GetForIpAsync(ipAddress) : GetForUserAsync(userId);
}
