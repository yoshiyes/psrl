using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Passerelle.Domain.Entities;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/links")]
[Authorize(Roles = "Admin")]
public class LinksController(ILinkRepository linkRepository, ICategoryRepository categoryRepository) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? category, string? sort = null, int page = 1,
        string? message = null)
    {
        string effectiveSort = sort ?? (!string.IsNullOrWhiteSpace(q) ? "relevance" : "newest");
        const int pageSize = 10;

        (var items, int total) =
            await linkRepository.SearchAsync(q, category is null ? null : [category], effectiveSort, page, pageSize);

        var vm = new AdminIndexViewModel
        {
            Links = items,
            Categories = await categoryRepository.GetAllAsync(),
            Query = q,
            SelectedCategory = category,
            Sort = effectiveSort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Message = message
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) && isHtmx == "true")
            return PartialView("_LinksResults", vm);

        return View(vm);
    }

    [HttpGet("create")]
    public async Task<IActionResult> CreateLink()
    {
        return PartialView("_LinkForm",
            new LinkFormViewModel { AvailableCategories = await categoryRepository.GetAllAsync() });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLink(LinkFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await categoryRepository.GetAllAsync();
            return PartialView("_LinkForm", model);
        }

        await linkRepository.AddAsync(new Link
        {
            Title = model.Title,
            Url = model.Url,
            Description = model.Description
        }, model.SelectedCategoryIds);

        Response.Headers["HX-Refresh"] = "true";

        return Content(string.Empty);
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> EditLink(Guid id)
    {
        Link? link = await linkRepository.GetByIdAsync(id);
        if (link is null) return NotFound();

        return PartialView("_LinkForm", new LinkFormViewModel
        {
            Id = link.Id,
            Title = link.Title,
            Url = link.Url,
            Description = link.Description,
            SelectedCategoryIds = link.LinkCategories.Select(x => x.CategoryId).ToList(),
            AvailableCategories = await categoryRepository.GetAllAsync()
        });
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLink(Guid id, LinkFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await categoryRepository.GetAllAsync();
            return PartialView("_LinkForm", model);
        }

        await linkRepository.UpdateAsync(new Link
        {
            Id = id,
            Title = model.Title,
            Url = model.Url,
            Description = model.Description
        }, model.SelectedCategoryIds);

        Response.Headers["HX-Refresh"] = "true";

        return Content(string.Empty);
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLink(Guid id)
    {
        await linkRepository.DeleteAsync(id);
        Response.Headers["HX-Refresh"] = "true";

        return Content(string.Empty);
    }
}
