using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Passerelle.Domain.Entities;
using Passerelle.Host.ViewModels;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Passerelle.Host.Controllers;

public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AuthController> logger,
    IStringLocalizer<SharedResource> localizer)
    : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl ??= Url.Action("Index", "Home");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
        
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, localizer["Auth_InvalidCredentials"]);
            return View(model);
        }

        SignInResult result = await signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation($"Utilisateur connecté : {model.Email}");

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, localizer["Auth_AccountLockedOut"]);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, localizer["Auth_InvalidCredentials"]);
        return View(model);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("Utilisateur déconnecté.");

        return RedirectToAction("Index", "Home");
    }

}