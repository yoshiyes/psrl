using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Controllers;

public sealed class HomeController(ILinkRepository linkRepository, ICategoryRepository categoryRepository) : Controller
{
    public async Task<IActionResult> Index(string? q, string[]? category, string? sort = null, int page = 1)
    {
        string effectiveSort = sort ?? (!string.IsNullOrWhiteSpace(q) ? "relevance" : "newest");
        const int pageSize = 10;

        (var items, int total) = await linkRepository.SearchAsync(q, category, effectiveSort, page, pageSize);

        var vm = new HomeIndexViewModel
        {
            Links = items,
            Categories = await categoryRepository.GetAllAsync(),
            Query = q,
            SelectedCategories = category ?? [],
            Sort = effectiveSort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) && isHtmx == "true")
            return PartialView("_LinksResults", vm);

        return View(vm);
    }
}
