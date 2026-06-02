using BLGNTube.Web.Data;
using BLGNTube.Web.Models;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BLGNTube.Web.Controllers;

/// <summary>Üyenin profili: kalan hak, istatistikler ve indirme geçmişi.</summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly QuotaService _quota;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        QuotaService quota)
    {
        _userManager = userManager;
        _db = db;
        _quota = quota;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var quota = await _quota.GetForUserAsync(user.Id);

        var history = await _db.DownloadRecords
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync();

        var total = await _db.DownloadRecords.CountAsync(r => r.UserId == user.Id);

        var model = new ProfileViewModel
        {
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            MemberSince = user.CreatedAt,
            UsedToday = quota.Used,
            DailyLimit = quota.Limit,
            TotalDownloads = total,
            History = history
        };

        return View(model);
    }
}
