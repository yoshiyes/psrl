using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/settings")]
[Authorize(Roles = "Admin")]
public sealed class SettingsController(ISiteSettingsService siteSettingsService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(bool saved = false, CancellationToken cancellationToken = default)
    {
        bool isGalleryEnabled = await siteSettingsService.IsGalleryEnabledAsync(cancellationToken);

        return View(new AdminSettingsViewModel
        {
            IsGalleryEnabled = isGalleryEnabled,
            Saved = saved
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminSettingsViewModel model, CancellationToken cancellationToken = default)
    {
        await siteSettingsService.UpdateGalleryEnabledAsync(model.IsGalleryEnabled, cancellationToken);

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) && isHtmx == "true")
        {
            Response.Headers["HX-Redirect"] = "/admin/settings?saved=true";
            return Content(string.Empty);
        }

        return RedirectToAction(nameof(Index), new { saved = true });
    }
}
