using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Passerelle.Host.Controllers;

public sealed class CultureController : Controller
{
    private static readonly string[] SupportedCultures = ["fr", "en"];

    [HttpPost]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(culture) || !SupportedCultures.Contains(culture))
        {
            culture = "fr";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });

        return LocalRedirect(returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
