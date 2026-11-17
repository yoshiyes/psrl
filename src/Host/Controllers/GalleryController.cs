using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Passerelle.Domain.Entities;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Controllers;

[Route("gallery")]
public sealed class GalleryController(
    IGalleryRepository galleryRepository,
    IImageTagRepository tagRepository,
    IGalleryStorageService storageService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? type, string[]? tag, string? sort = null, int page = 1)
    {
        string effectiveSort = sort ?? (!string.IsNullOrWhiteSpace(q) ? "relevance" : "newest");
        const int pageSize = 12;

        GalleryItemType? parsedType = ParseType(type);

        (var items, int total) = await galleryRepository.SearchAsync(
            q,
            parsedType,
            tag,
            effectiveSort,
            page,
            pageSize);

        var vm = new GalleryIndexViewModel
        {
            Items = items,
            Tags = await tagRepository.GetAllAsync(),
            Query = q,
            SelectedType = parsedType?.ToString().ToLowerInvariant(),
            SelectedTags = tag ?? [],
            Sort = effectiveSort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) &&
            isHtmx == "true" &&
            !Request.Headers.ContainsKey("HX-History-Restore-Request"))
        {
            return PartialView("_GalleryResults", vm);
        }

        return View(vm);
    }

    [HttpGet("preview/{id:guid}")]
    public async Task<IActionResult> Preview(Guid id)
    {
        GalleryItem? item = await galleryRepository.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        return PartialView("_GalleryPreviewModal", item);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        GalleryItem? item = await galleryRepository.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        return View(item);
    }

    [HttpGet("media/{id:guid}/{variant}")]
    public async Task<IActionResult> Media(Guid id, string variant)
    {
        GalleryItem? item = await galleryRepository.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        (string? filePath, string contentType, string? downloadFileName) =
            storageService.ResolveMediaFile(item, variant);

        if (filePath is null)
            return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";

        if (!string.IsNullOrWhiteSpace(downloadFileName))
            return PhysicalFile(filePath, contentType, downloadFileName);

        return PhysicalFile(filePath, contentType);
    }

    private static GalleryItemType? ParseType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant() switch
        {
            "image" => GalleryItemType.Image,
            "photo" => GalleryItemType.Photo,
            _ => null
        };
    }
}
