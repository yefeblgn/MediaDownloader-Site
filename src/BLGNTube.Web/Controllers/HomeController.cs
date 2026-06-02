using System.Security.Claims;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BLGNTube.Web.Controllers;

/// <summary>Açılış sayfası (indirme arayüzü) ve statik bilgi sayfaları.</summary>
public class HomeController : Controller
{
    private readonly QuotaService _quota;

    public HomeController(QuotaService quota) => _quota = quota;

    public async Task<IActionResult> Index()
    {
        // Kalan günlük hak bilgisini arayüzde göstermek için ViewBag'e koy.
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var quota = await _quota.GetAsync(userId, ip);
        ViewBag.Remaining = quota.Remaining;
        ViewBag.Limit = quota.Limit;
        ViewBag.IsAuthenticated = userId is not null;
        return View();
    }

    public IActionResult About() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
