using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using Passerelle.Domain.Entities;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoriesController(
    ICategoryRepository categoryRepository,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string sort = "name_asc", int page = 1, string? message = null)
    {
        const int pageSize = 10;
        ViewData["Message"] = message;

        (var items, int total) = await categoryRepository.SearchAsync(q, sort, page, pageSize);

        var vm = new CategoriesIndexViewModel
        {
            Categories = items,
            Query = q,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) && isHtmx == "true" && !Request.Headers.ContainsKey("HX-History-Restore-Request"))
            return PartialView("_CategoriesResults", vm);

        return View(vm);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return PartialView("_CategoryForm", new CategoryFormViewModel());
    }

    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
        return PartialView("_CategoryFormPlaceholder");
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return PartialView("_CategoryForm", model);

        await categoryRepository.AddAsync(new Category
        {
            Name = model.Name,
            Slug = model.Name,
            Emoji = NormalizeEmoji(model.Emoji)
        });

        ViewData["Message"] = localizer["Admin_Categories_Created"].Value;
        Response.Headers["HX-Trigger"] = "categories-changed";

        return PartialView("_CategoryFormPlaceholder");
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        Category? category = await categoryRepository.GetByIdAsync(id);

        if (category is null) return NotFound();

        return PartialView("_CategoryForm", new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Emoji = category.Emoji
        });
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return PartialView("_CategoryForm", model);

        await categoryRepository.UpdateAsync(new Category
        {
            Id = id,
            Name = model.Name,
            Slug = model.Name,
            Emoji = NormalizeEmoji(model.Emoji)
        });

        ViewData["Message"] = localizer["Admin_Categories_Updated"].Value;
        Response.Headers["HX-Trigger"] = "categories-changed";

        return PartialView("_CategoryFormPlaceholder");
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (await categoryRepository.HasLinkedItemsAsync(id))
        {
            ViewData["Message"] = localizer["Admin_Categories_DeleteBlocked"].Value;

            return PartialView("_CategoryFormPlaceholder");
        }

        await categoryRepository.DeleteAsync(id);

        ViewData["Message"] = localizer["Admin_Categories_Deleted"].Value;
        Response.Headers["HX-Trigger"] = "categories-changed";

        return PartialView("_CategoryFormPlaceholder");
    }

    private static string? NormalizeEmoji(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}