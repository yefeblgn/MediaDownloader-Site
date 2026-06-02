using BLGNTube.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace BLGNTube.Web.Services;

/// <summary>Bir kullanıcı/IP için günlük kota durumunu özetler.</summary>
public record QuotaStatus(int Used, int Limit)
{
    public int Remaining => Math.Max(0, Limit - Used);
    public bool CanDownload => Used < Limit;
}

/// <summary>
/// Günlük indirme limitlerini yönetir.
/// Anonim kullanıcılar IP adresine göre günde <see cref="AnonymousDailyLimit"/>,
/// üye kullanıcılar günde <see cref="AuthenticatedDailyLimit"/> indirme yapabilir.
/// Kullanım, o güne ait <see cref="Models.DownloadRecord"/> sayısından hesaplanır.
/// </summary>
public class QuotaService
{
    public const int AnonymousDailyLimit = 10;
    public const int AuthenticatedDailyLimit = 100;

    private readonly ApplicationDbContext _db;

    public QuotaService(ApplicationDbContext db) => _db = db;

    /// <summary>Verilen kimliğe göre uygulanacak günlük limiti döndürür.</summary>
    public static int LimitFor(bool authenticated) =>
        authenticated ? AuthenticatedDailyLimit : AnonymousDailyLimit;

    /// <summary>UTC gününün başlangıcı (kota penceresi günlük olarak sıfırlanır).</summary>
    private static DateTime TodayStartUtc => DateTime.UtcNow.Date;

    /// <summary>Üye kullanıcının bugünkü kota durumunu getirir.</summary>
    public async Task<QuotaStatus> GetForUserAsync(string userId)
    {
        var used = await _db.DownloadRecords
            .CountAsync(r => r.UserId == userId && r.CreatedAt >= TodayStartUtc);
        return new QuotaStatus(used, AuthenticatedDailyLimit);
    }

    /// <summary>Anonim (IP bazlı) kota durumunu getirir.</summary>
    public async Task<QuotaStatus> GetForIpAsync(string ipAddress)
    {
        var used = await _db.DownloadRecords
            .CountAsync(r => r.UserId == null && r.IpAddress == ipAddress && r.CreatedAt >= TodayStartUtc);
        return new QuotaStatus(used, AnonymousDailyLimit);
    }

    /// <summary>İsteğin sahibine (üye ya da IP) göre kota durumunu getirir.</summary>
    public Task<QuotaStatus> GetAsync(string? userId, string ipAddress) =>
        string.IsNullOrEmpty(userId) ? GetForIpAsync(ipAddress) : GetForUserAsync(userId);
}
