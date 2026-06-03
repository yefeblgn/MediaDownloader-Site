using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BLGNTube.Web.Controllers;

public class CultureController : Controller
{
    [HttpGet]
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        if (culture is not ("tr" or "en")) culture = "tr";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }
}
