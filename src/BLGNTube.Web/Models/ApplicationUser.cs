using Microsoft.AspNetCore.Identity;

namespace BLGNTube.Web.Models;

/// <summary>
/// BLGNTube kullanıcısı. ASP.NET Core Identity'nin IdentityUser sınıfını
/// görünen ad ve kayıt tarihi gibi ek alanlarla genişletir.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Profilde ve menüde gösterilen ad.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Hesabın oluşturulduğu tarih (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Bu kullanıcının yaptığı indirme kayıtları.</summary>
    public ICollection<DownloadRecord> Downloads { get; set; } = new List<DownloadRecord>();
}
