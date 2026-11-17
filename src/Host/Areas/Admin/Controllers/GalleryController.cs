using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using Passerelle.Domain.Entities;
using Passerelle.Host.Interfaces;
using Passerelle.Host.ViewModels;

namespace Passerelle.Host.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/gallery")]
[Authorize(Roles = "Admin")]
public sealed class GalleryController(
    IGalleryRepository galleryRepository,
    IImageTagRepository tagRepository,
    IGalleryStorageService storageService,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? q,
        string? type,
        string? tag,
        string? sort = null,
        int page = 1,
        string? message = null)
    {
        string effectiveSort = sort ?? (!string.IsNullOrWhiteSpace(q) ? "relevance" : "newest");
        const int pageSize = 10;

        GalleryItemType? parsedType = ParseType(type);

        (var items, int total) = await galleryRepository.SearchAsync(
            q,
            parsedType,
            string.IsNullOrWhiteSpace(tag) ? null : [tag],
            effectiveSort,
            page,
            pageSize);

        var vm = new AdminGalleryIndexViewModel
        {
            Items = items,
            Tags = await tagRepository.GetAllAsync(),
            Query = q,
            SelectedType = parsedType?.ToString().ToLowerInvariant(),
            SelectedTag = tag,
            Sort = effectiveSort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Message = message
        };

        if (Request.Headers.TryGetValue("HX-Request", out StringValues isHtmx) &&
            isHtmx == "true" &&
            !Request.Headers.ContainsKey("HX-History-Restore-Request"))
        {
            return PartialView("_GalleryResults", vm);
        }

        return View(vm);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        return View(new GalleryFormViewModel
        {
            AvailableTags = await tagRepository.GetAllAsync()
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)] // 50 MB max upload
    public async Task<IActionResult> Create(GalleryFormViewModel model, CancellationToken cancellationToken)
    {
        bool isHtmx = Request.Headers.TryGetValue("HX-Request", out StringValues hxVal) && hxVal == "true";

        if (model.ImageFile is null || model.ImageFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.ImageFile), localizer["Validation_ImageFile_Required"].Value);
        }
        else if (!storageService.IsSupportedImage(model.ImageFile))
        {
            ModelState.AddModelError(nameof(model.ImageFile), localizer["Validation_ImageFile_Invalid"].Value);
        }

        if (!ModelState.IsValid)
        {
            model.AvailableTags = await tagRepository.GetAllAsync();
            return isHtmx ? PartialView("_GalleryForm", model) : View(model);
        }

        ProcessedGalleryUpload processed;
        try
        {
            processed = await storageService.ProcessUploadAsync(
                model.ImageFile!,
                model.Type,
                model.LocationMode,
                model.Latitude,
                model.Longitude,
                cancellationToken);
        }
        catch
        {
            ModelState.AddModelError(nameof(model.ImageFile), localizer["Validation_ImageFile_Invalid"].Value);
            model.AvailableTags = await tagRepository.GetAllAsync();
            return isHtmx ? PartialView("_GalleryForm", model) : View(model);
        }

        IReadOnlyCollection<Guid> resolvedTagIds = await tagRepository.ResolveTagIdsAsync(
            model.SelectedTagIds,
            model.NewTags);

        string title = string.IsNullOrWhiteSpace(model.Title)
            ? Path.GetFileNameWithoutExtension(processed.OriginalFileName)
            : model.Title.Trim();

        if (title.Length > 160)
            title = title[..160];

        string? locationName = model.Type == GalleryItemType.Photo && model.LocationMode != GalleryLocationMode.Hidden
            ? NormalizeNullable(model.LocationName)
            : null;

        await galleryRepository.AddAsync(new GalleryItem
        {
            Title = title,
            Description = model.Description?.Trim() ?? string.Empty,
            Type = model.Type,
            OriginalFileName = processed.OriginalFileName,
            StoredFileName = processed.StoredFileName,
            ThumbnailFileName = processed.ThumbnailFileName,
            PreviewFileName = processed.PreviewFileName,
            ContentType = processed.ContentType,
            FileSizeBytes = processed.FileSizeBytes,
            Width = processed.Width,
            Height = processed.Height,
            CameraMake = processed.CameraMake,
            CameraModel = processed.CameraModel,
            LensModel = processed.LensModel,
            FocalLength = processed.FocalLength,
            Aperture = processed.Aperture,
            ShutterSpeed = processed.ShutterSpeed,
            Iso = processed.Iso,
            TakenAt = processed.TakenAt,
            LocationMode = model.Type == GalleryItemType.Photo ? model.LocationMode : GalleryLocationMode.Hidden,
            LocationName = locationName,
            Latitude = processed.Latitude,
            Longitude = processed.Longitude
        }, resolvedTagIds);

        if (isHtmx)
        {
            Response.Headers["HX-Redirect"] = "/admin/gallery";
            return Content(string.Empty);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        GalleryItem? item = await galleryRepository.GetByIdAsync(id);
        if (item is null)
            return NotFound();

        return View(new GalleryFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Type = item.Type,
            LocationMode = item.LocationMode,
            LocationName = item.LocationName,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            SelectedTagIds = item.GalleryItemTags.Select(x => x.ImageTagId).ToList(),
            AvailableTags = await tagRepository.GetAllAsync()
        });
    }

    [HttpPost("edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Edit(Guid id, GalleryFormViewModel model, CancellationToken cancellationToken)
    {
        bool isHtmx = Request.Headers.TryGetValue("HX-Request", out StringValues hxVal) && hxVal == "true";

        GalleryItem? existing = await galleryRepository.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (model.ImageFile is { Length: > 0 } && !storageService.IsSupportedImage(model.ImageFile))
        {
            ModelState.AddModelError(nameof(model.ImageFile), localizer["Validation_ImageFile_Invalid"].Value);
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.AvailableTags = await tagRepository.GetAllAsync();
            return isHtmx ? PartialView("_GalleryForm", model) : View(model);
        }

        IReadOnlyCollection<Guid> resolvedTagIds = await tagRepository.ResolveTagIdsAsync(
            model.SelectedTagIds,
            model.NewTags);

        string title = string.IsNullOrWhiteSpace(model.Title)
            ? existing.Title
            : model.Title.Trim();

        if (title.Length > 160)
            title = title[..160];

        var updated = new GalleryItem
        {
            Id = existing.Id,
            Title = title,
            Description = model.Description?.Trim() ?? string.Empty,
            Type = model.Type,
            OriginalFileName = existing.OriginalFileName,
            StoredFileName = existing.StoredFileName,
            ThumbnailFileName = existing.ThumbnailFileName,
            PreviewFileName = existing.PreviewFileName,
            ContentType = existing.ContentType,
            FileSizeBytes = existing.FileSizeBytes,
            Width = existing.Width,
            Height = existing.Height,
            CameraMake = model.Type == GalleryItemType.Photo ? existing.CameraMake : null,
            CameraModel = model.Type == GalleryItemType.Photo ? existing.CameraModel : null,
            LensModel = model.Type == GalleryItemType.Photo ? existing.LensModel : null,
            FocalLength = model.Type == GalleryItemType.Photo ? existing.FocalLength : null,
            Aperture = model.Type == GalleryItemType.Photo ? existing.Aperture : null,
            ShutterSpeed = model.Type == GalleryItemType.Photo ? existing.ShutterSpeed : null,
            Iso = model.Type == GalleryItemType.Photo ? existing.Iso : null,
            TakenAt = model.Type == GalleryItemType.Photo ? existing.TakenAt : null,
            LocationMode = model.Type == GalleryItemType.Photo ? model.LocationMode : GalleryLocationMode.Hidden,
            LocationName = model.Type == GalleryItemType.Photo && model.LocationMode != GalleryLocationMode.Hidden
                ? NormalizeNullable(model.LocationName)
                : null,
            Latitude = model.Type == GalleryItemType.Photo && model.LocationMode != GalleryLocationMode.Hidden
                ? (model.LocationMode == GalleryLocationMode.Overridden ? model.Latitude : existing.Latitude)
                : null,
            Longitude = model.Type == GalleryItemType.Photo && model.LocationMode != GalleryLocationMode.Hidden
                ? (model.LocationMode == GalleryLocationMode.Overridden ? model.Longitude : existing.Longitude)
                : null
        };

        if (model.ImageFile is { Length: > 0 })
        {
            ProcessedGalleryUpload processed;
            try
            {
                processed = await storageService.ProcessUploadAsync(
                    model.ImageFile,
                    model.Type,
                    model.LocationMode,
                    model.Latitude,
                    model.Longitude,
                    cancellationToken);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ImageFile), localizer["Validation_ImageFile_Invalid"].Value);
                model.Id = id;
                model.AvailableTags = await tagRepository.GetAllAsync();
                return isHtmx ? PartialView("_GalleryForm", model) : View(model);
            }

            storageService.DeleteFiles(existing.StoredFileName, existing.ThumbnailFileName, existing.PreviewFileName);

            updated.OriginalFileName = processed.OriginalFileName;
            updated.StoredFileName = processed.StoredFileName;
            updated.ThumbnailFileName = processed.ThumbnailFileName;
            updated.PreviewFileName = processed.PreviewFileName;
            updated.ContentType = processed.ContentType;
            updated.FileSizeBytes = processed.FileSizeBytes;
            updated.Width = processed.Width;
            updated.Height = processed.Height;
            updated.CameraMake = processed.CameraMake;
            updated.CameraModel = processed.CameraModel;
            updated.LensModel = processed.LensModel;
            updated.FocalLength = processed.FocalLength;
            updated.Aperture = processed.Aperture;
            updated.ShutterSpeed = processed.ShutterSpeed;
            updated.Iso = processed.Iso;
            updated.TakenAt = processed.TakenAt;
            updated.Latitude = processed.Latitude;
            updated.Longitude = processed.Longitude;
        }
        else
        {
            long? updatedFileSize = await storageService.UpdateStoredFileLocationMetadataAsync(
                existing.StoredFileName,
                updated.Type,
                updated.LocationMode,
                updated.Latitude,
                updated.Longitude,
                cancellationToken);

            if (updatedFileSize.HasValue)
                updated.FileSizeBytes = updatedFileSize.Value;
        }

        await galleryRepository.UpdateAsync(updated, resolvedTagIds);

        if (isHtmx)
        {
            Response.Headers["HX-Redirect"] = "/admin/gallery";
            return Content(string.Empty);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        GalleryItem? existing = await galleryRepository.GetByIdAsync(id);
        if (existing is not null)
        {
            storageService.DeleteFiles(existing.StoredFileName, existing.ThumbnailFileName, existing.PreviewFileName);
            await galleryRepository.DeleteAsync(id);
        }

        Response.Headers["HX-Refresh"] = "true";
        return Content(string.Empty);
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

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
