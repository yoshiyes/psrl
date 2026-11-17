using Microsoft.AspNetCore.Http;
using NodaTime;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public sealed record ProcessedGalleryUpload(
    string OriginalFileName,
    string StoredFileName,
    string ThumbnailFileName,
    string PreviewFileName,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height,
    string? CameraMake,
    string? CameraModel,
    string? LensModel,
    string? FocalLength,
    string? Aperture,
    string? ShutterSpeed,
    int? Iso,
    Instant? TakenAt,
    double? Latitude,
    double? Longitude);

public interface IGalleryStorageService
{
    bool IsSupportedImage(IFormFile file);

    Task<ProcessedGalleryUpload> ProcessUploadAsync(
        IFormFile file,
        GalleryItemType type,
        GalleryLocationMode locationMode,
        double? customLatitude,
        double? customLongitude,
        CancellationToken cancellationToken = default);

    Task<long?> UpdateStoredFileLocationMetadataAsync(
        string storedFileName,
        GalleryItemType type,
        GalleryLocationMode locationMode,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default);

    (string? FilePath, string ContentType, string? DownloadFileName) ResolveMediaFile(
        GalleryItem item,
        string variant);

    void DeleteFiles(string? storedFileName, string? thumbnailFileName, string? previewFileName);
}
