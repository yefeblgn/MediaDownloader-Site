namespace BLGNTube.Web.Models;

public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SiteCount
{
    public string SiteName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalDownloads { get; set; }
    public int DownloadsToday { get; set; }
    public int AnonymousToday { get; set; }

    public List<SiteCount> TopSites { get; set; } = new();
    public List<AdminUserRow> Users { get; set; } = new();
    public List<DownloadRecord> RecentDownloads { get; set; } = new();

    public string CurrentUserId { get; set; } = string.Empty;
}
