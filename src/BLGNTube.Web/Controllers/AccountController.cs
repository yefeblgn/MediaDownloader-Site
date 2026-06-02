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
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

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

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User)) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
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
        return View(model);
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
