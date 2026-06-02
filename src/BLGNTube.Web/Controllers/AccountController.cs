using System.Security.Claims;
using BLGNTube.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BLGNTube.Web.Controllers;

/// <summary>Kayıt, giriş ve çıkış işlemlerini yöneten controller.</summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        await PopulateExternalSchemesAsync();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            await PopulateExternalSchemesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));

        await PopulateExternalSchemesAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        await PopulateExternalSchemesAsync();
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded) return RedirectToLocal(returnUrl);

        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
        await PopulateExternalSchemesAsync();
        return View(model);
    }

    // --- Harici giriş (Google) ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            TempData["ExternalError"] = "Harici sağlayıcı hatası: " + remoteError;
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null) return RedirectToAction(nameof(Login), new { returnUrl });

        // Daha önce bağlanmışsa doğrudan giriş yap.
        var signIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (signIn.Succeeded) return RedirectToLocal(returnUrl);

        // Yeni kullanıcı: Google profilinden e-posta/ad al.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            TempData["ExternalError"] = "Google hesabından e-posta alınamadı.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                TempData["ExternalError"] = "Hesap oluşturulamadı.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: true);
        return RedirectToLocal(returnUrl);
    }

    /// <summary>Görünümlerde harici giriş butonlarını göstermek için şemaları doldurur.</summary>
    private async Task PopulateExternalSchemesAsync()
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        ViewData["ExternalSchemes"] = schemes.ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Bu e-posta zaten kayıtlı.",
        "PasswordTooShort" => "Şifre çok kısa (en az 6 karakter).",
        "PasswordRequiresNonAlphanumeric" => "Şifre en az bir özel karakter içermeli.",
        "PasswordRequiresDigit" => "Şifre en az bir rakam içermeli.",
        "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermeli.",
        "PasswordRequiresLower" => "Şifre en az bir küçük harf içermeli.",
        _ => error.Description
    };
}
