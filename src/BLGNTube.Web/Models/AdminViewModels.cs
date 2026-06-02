namespace BLGNTube.Web.Models;

/// <summary>Admin panelindeki tek bir kullanıcı satırı.</summary>
public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>En çok indirilen site özeti.</summary>
public class SiteCount
{
    public string SiteName { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Admin panelinin görüntü modeli.</summary>
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalDownloads { get; set; }
    public int DownloadsToday { get; set; }
    public int AnonymousToday { get; set; }

    public List<SiteCount> TopSites { get; set; } = new();
    public List<AdminUserRow> Users { get; set; } = new();
    public List<DownloadRecord> RecentDownloads { get; set; } = new();

    /// <summary>Giriş yapan adminin kendi kimliği (kendi rolünü almasını engellemek için).</summary>
    public string CurrentUserId { get; set; } = string.Empty;
}
