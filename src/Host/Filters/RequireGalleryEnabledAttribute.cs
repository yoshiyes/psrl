using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Passerelle.Host.Interfaces;

namespace Passerelle.Host.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireGalleryEnabledAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var settingsService = context.HttpContext.RequestServices.GetRequiredService<ISiteSettingsService>();

        if (!await settingsService.IsGalleryEnabledAsync(context.HttpContext.RequestAborted))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
