using Microsoft.AspNetCore.Identity;

namespace BLGNTube.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DownloadRecord> Downloads { get; set; } = new List<DownloadRecord>();
}
