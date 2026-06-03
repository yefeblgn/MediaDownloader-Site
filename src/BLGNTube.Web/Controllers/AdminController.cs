using BLGNTube.Web.Data;
using BLGNTube.Web.Models;
using BLGNTube.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BLGNTube.Web.Controllers;

[Authorize(Roles = IdentitySeeder.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var todayStart = DateTime.UtcNow.Date;

        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _db.Users.CountAsync(),
            TotalDownloads = await _db.DownloadRecords.CountAsync(),
            DownloadsToday = await _db.DownloadRecords.CountAsync(r => r.CreatedAt >= todayStart),
            AnonymousToday = await _db.DownloadRecords.CountAsync(r => r.UserId == null && r.CreatedAt >= todayStart),
            CurrentUserId = _userManager.GetUserId(User) ?? string.Empty
        };

        model.TopSites = await _db.DownloadRecords
            .Where(r => r.SiteName != null)
            .GroupBy(r => r.SiteName!)
            .Select(g => new SiteCount { SiteName = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count)
            .Take(8)
            .ToListAsync();

        model.RecentDownloads = await _db.DownloadRecords
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        var users = await _db.Users.OrderByDescending(u => u.CreatedAt).Take(50).ToListAsync();
        var counts = await _db.DownloadRecords
            .Where(r => r.UserId != null)
            .GroupBy(r => r.UserId!)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var adminUsers = await _userManager.GetUsersInRoleAsync(IdentitySeeder.AdminRole);
        var adminIds = adminUsers.Select(u => u.Id).ToHashSet();

        model.Users = users.Select(u => new AdminUserRow
        {
            Id = u.Id,
            DisplayName = u.DisplayName,
            Email = u.Email ?? string.Empty,
            IsAdmin = adminIds.Contains(u.Id),
            DownloadCount = counts.TryGetValue(u.Id, out var c) ? c : 0,
            CreatedAt = u.CreatedAt
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        var record = await _db.DownloadRecords.FindAsync(id);
        if (record is not null)
        {
            _db.DownloadRecords.Remove(record);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string userId)
    {
        if (userId == _userManager.GetUserId(User))
            return RedirectToAction(nameof(Index));

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            if (await _userManager.IsInRoleAsync(user, IdentitySeeder.AdminRole))
                await _userManager.RemoveFromRoleAsync(user, IdentitySeeder.AdminRole);
            else
                await _userManager.AddToRoleAsync(user, IdentitySeeder.AdminRole);
        }
        return RedirectToAction(nameof(Index));
    }
}
