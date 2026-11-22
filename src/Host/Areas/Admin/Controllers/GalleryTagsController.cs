using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using Passerelle.Domain.Entities;
using Passerelle.Host.Filters;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/gallery/tags")]
[Authorize(Roles = "Admin")]
[RequireGalleryEnabled]
public sealed class GalleryTagsController(
    IImageTagRepository tagRepository,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string sort = "name_asc", int page = 1, string? message = null)
    {
        const int pageSize = 10;
        ViewData["Message"] = message;

        (var items, int total) = await tagRepository.SearchAsync(q, sort, page, pageSize);

        var vm = new ImageTagsIndexViewModel
        {
            Tags = items,
            Query = q,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) &&
            isHtmx == "true" &&
            !Request.Headers.ContainsKey("HX-History-Restore-Request"))
        {
            return PartialView("_TagsResults", vm);
        }

        return View(vm);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return PartialView("_TagForm", new ImageTagFormViewModel());
    }

    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
        return PartialView("_TagFormPlaceholder");
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImageTagFormViewModel model)
    {
        if (!ModelState.IsValid)
            return PartialView("_TagForm", model);

        await tagRepository.AddAsync(new ImageTag
        {
            Name = model.Name,
            Slug = model.Name,
            Emoji = NormalizeEmoji(model.Emoji)
        });

        ViewData["Message"] = localizer["Admin_Tags_Created"].Value;
        Response.Headers["HX-Trigger"] = "tags-changed";

        return PartialView("_TagFormPlaceholder");
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        ImageTag? tag = await tagRepository.GetByIdAsync(id);

        if (tag is null)
            return NotFound();

        return PartialView("_TagForm", new ImageTagFormViewModel
        {
            Id = tag.Id,
            Name = tag.Name,
            Emoji = tag.Emoji
        });
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ImageTagFormViewModel model)
    {
        if (!ModelState.IsValid)
            return PartialView("_TagForm", model);

        await tagRepository.UpdateAsync(new ImageTag
        {
            Id = id,
            Name = model.Name,
            Slug = model.Name,
            Emoji = NormalizeEmoji(model.Emoji)
        });

        ViewData["Message"] = localizer["Admin_Tags_Updated"].Value;
        Response.Headers["HX-Trigger"] = "tags-changed";

        return PartialView("_TagFormPlaceholder");
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await tagRepository.HasLinkedItemsAsync(id))
        {
            ViewData["Message"] = localizer["Admin_Tags_DeleteBlocked"].Value;
            return PartialView("_TagFormPlaceholder");
        }

        await tagRepository.DeleteAsync(id);

        ViewData["Message"] = localizer["Admin_Tags_Deleted"].Value;
        Response.Headers["HX-Trigger"] = "tags-changed";

        return PartialView("_TagFormPlaceholder");
    }

    private static string? NormalizeEmoji(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
