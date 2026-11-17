using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using NodaTime;
using Passerelle.Domain.Entities;
using Passerelle.Host.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace Passerelle.Host.Services;

public sealed class GalleryStorageService : IGalleryStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".tif"
    };

    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private const int ThumbnailMaxDimension = 480;
    private const int PreviewMaxDimension = 1600;

    private readonly string _storageRoot;

    public GalleryStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        string configuredPath = configuration["Gallery:StoragePath"] ?? "data/gallery";
        _storageRoot = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(_storageRoot);
    }

    public bool IsSupportedImage(IFormFile file)
    {
        if (file.Length <= 0)
            return false;

        string ext = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(ext) && AllowedExtensions.Contains(ext);
    }

    public async Task<ProcessedGalleryUpload> ProcessUploadAsync(
        IFormFile file,
        GalleryItemType type,
        GalleryLocationMode locationMode,
        double? customLatitude,
        double? customLongitude,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_storageRoot);

        string fileId = Guid.NewGuid().ToString("N");
        string ext = NormalizeExtension(Path.GetExtension(file.FileName));
        string storedFileName = $"{fileId}{ext}";
        string thumbnailFileName = $"{fileId}_thumb.webp";
        string previewFileName = $"{fileId}_preview.webp";

        string storedPath = Path.Combine(_storageRoot, storedFileName);
        string thumbPath = Path.Combine(_storageRoot, thumbnailFileName);
        string previewPath = Path.Combine(_storageRoot, previewFileName);

        await using Stream inputStream = file.OpenReadStream();
        using Image image = await Image.LoadAsync(inputStream, cancellationToken);

        ExifProfile? exif = image.Metadata.ExifProfile;

        string? cameraMake = null;
        string? cameraModel = null;
        string? lensModel = null;
        string? focalLength = null;
        string? aperture = null;
        string? shutterSpeed = null;
        int? iso = null;
        Instant? takenAt = null;
        double? extractedLat = null;
        double? extractedLon = null;

        if (exif is not null)
        {
            cameraMake = CleanExifString(GetExifString(exif, ExifTag.Make), 100);
            cameraModel = CleanExifString(GetExifString(exif, ExifTag.Model), 100);
            lensModel = CleanExifString(GetExifString(exif, ExifTag.LensModel), 160);
            focalLength = FormatFocalLength(exif);
            aperture = FormatAperture(exif);
            shutterSpeed = FormatShutterSpeed(exif);
            iso = ExtractIso(exif);
            takenAt = ExtractTakenAt(exif);
            (extractedLat, extractedLon) = ExtractGpsCoordinates(exif);
        }

        double? finalLatitude = null;
        double? finalLongitude = null;

        if (type == GalleryItemType.Photo)
        {
            switch (locationMode)
            {
                case GalleryLocationMode.Original:
                    finalLatitude = extractedLat;
                    finalLongitude = extractedLon;
                    break;
                case GalleryLocationMode.Overridden:
                    finalLatitude = customLatitude;
                    finalLongitude = customLongitude;
                    ApplyGpsOverrideToExif(image, finalLatitude, finalLongitude);
                    break;
                case GalleryLocationMode.Hidden:
                    RemoveGpsFromExif(image.Metadata.ExifProfile);
                    break;
            }
        }
        else
        {
            RemoveGpsFromExif(image.Metadata.ExifProfile);
        }

        // Normalize orientation before measuring and generating derivatives
        image.Mutate(x => x.AutoOrient());
        int width = image.Width;
        int height = image.Height;

        // Save sanitized/updated original image
        await image.SaveAsync(storedPath, cancellationToken);
        long savedSize = new FileInfo(storedPath).Length;

        // Generate lightweight WebP thumbnail (max 480px, stripped metadata)
        using (Image thumb = image.Clone(ctx =>
               {
                   if (image.Width > ThumbnailMaxDimension || image.Height > ThumbnailMaxDimension)
                   {
                       ctx.Resize(new ResizeOptions
                       {
                           Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension),
                           Mode = ResizeMode.Max
                       });
                   }
               }))
        {
            StripAllMetadata(thumb);
            await thumb.SaveAsWebpAsync(thumbPath, new WebpEncoder { Quality = 78 }, cancellationToken);
        }

        // Generate lightweight WebP preview (max 1600px, stripped metadata)
        using (Image preview = image.Clone(ctx =>
               {
                   if (image.Width > PreviewMaxDimension || image.Height > PreviewMaxDimension)
                   {
                       ctx.Resize(new ResizeOptions
                       {
                           Size = new Size(PreviewMaxDimension, PreviewMaxDimension),
                           Mode = ResizeMode.Max
                       });
                   }
               }))
        {
            StripAllMetadata(preview);
            await preview.SaveAsWebpAsync(previewPath, new WebpEncoder { Quality = 84 }, cancellationToken);
        }

        string contentType = ContentTypeProvider.TryGetContentType(storedFileName, out string? resolvedType)
            ? resolvedType
            : "image/jpeg";

        return new ProcessedGalleryUpload(
            OriginalFileName: CleanExifString(Path.GetFileName(file.FileName), 260) ?? storedFileName,
            StoredFileName: storedFileName,
            ThumbnailFileName: thumbnailFileName,
            PreviewFileName: previewFileName,
            ContentType: contentType,
            FileSizeBytes: savedSize,
            Width: width,
            Height: height,
            CameraMake: type == GalleryItemType.Photo ? cameraMake : null,
            CameraModel: type == GalleryItemType.Photo ? cameraModel : null,
            LensModel: type == GalleryItemType.Photo ? lensModel : null,
            FocalLength: type == GalleryItemType.Photo ? focalLength : null,
            Aperture: type == GalleryItemType.Photo ? aperture : null,
            ShutterSpeed: type == GalleryItemType.Photo ? shutterSpeed : null,
            Iso: type == GalleryItemType.Photo ? iso : null,
            TakenAt: type == GalleryItemType.Photo ? takenAt : null,
            Latitude: finalLatitude,
            Longitude: finalLongitude);
    }

    public async Task<long?> UpdateStoredFileLocationMetadataAsync(
        string storedFileName,
        GalleryItemType type,
        GalleryLocationMode locationMode,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            return null;

        string safeName = Path.GetFileName(storedFileName);
        string storedPath = Path.Combine(_storageRoot, safeName);

        if (!File.Exists(storedPath))
            return null;

        using Image image = await Image.LoadAsync(storedPath, cancellationToken);

        if (type != GalleryItemType.Photo || locationMode == GalleryLocationMode.Hidden)
        {
            RemoveGpsFromExif(image.Metadata.ExifProfile);
        }
        else if (locationMode == GalleryLocationMode.Overridden)
        {
            ApplyGpsOverrideToExif(image, latitude, longitude);
        }

        await image.SaveAsync(storedPath, cancellationToken);
        return new FileInfo(storedPath).Length;
    }

    public (string? FilePath, string ContentType, string? DownloadFileName) ResolveMediaFile(
        GalleryItem item,
        string variant)
    {
        string normalizedVariant = variant.Trim().ToLowerInvariant();
        string fileName = normalizedVariant switch
        {
            "thumb" => item.ThumbnailFileName,
            "preview" => item.PreviewFileName,
            "original" => item.StoredFileName,
            _ => item.PreviewFileName
        };

        string safeName = Path.GetFileName(fileName);
        string fullPath = Path.Combine(_storageRoot, safeName);

        if (!File.Exists(fullPath))
            return (null, "application/octet-stream", null);

        if (normalizedVariant == "original")
        {
            string downloadName = string.IsNullOrWhiteSpace(item.OriginalFileName)
                ? safeName
                : item.OriginalFileName;
            return (fullPath, item.ContentType, downloadName);
        }

        return (fullPath, "image/webp", null);
    }

    public void DeleteFiles(string? storedFileName, string? thumbnailFileName, string? previewFileName)
    {
        DeleteSingleFile(storedFileName);
        DeleteSingleFile(thumbnailFileName);
        DeleteSingleFile(previewFileName);
    }

    private void DeleteSingleFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            string safeName = Path.GetFileName(fileName);
            string fullPath = Path.Combine(_storageRoot, safeName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Ignore file deletion errors
        }
    }

    private static void StripAllMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;
    }

    private static void RemoveGpsFromExif(ExifProfile? exif)
    {
        if (exif is null)
            return;

        exif.RemoveValue(ExifTag.GPSVersionID);
        exif.RemoveValue(ExifTag.GPSLatitudeRef);
        exif.RemoveValue(ExifTag.GPSLatitude);
        exif.RemoveValue(ExifTag.GPSLongitudeRef);
        exif.RemoveValue(ExifTag.GPSLongitude);
        exif.RemoveValue(ExifTag.GPSAltitudeRef);
        exif.RemoveValue(ExifTag.GPSAltitude);
        exif.RemoveValue(ExifTag.GPSTimestamp);
        exif.RemoveValue(ExifTag.GPSDateStamp);
        exif.RemoveValue(ExifTag.GPSMapDatum);
        exif.RemoveValue(ExifTag.GPSProcessingMethod);
        exif.RemoveValue(ExifTag.GPSAreaInformation);
    }

    private static void ApplyGpsOverrideToExif(Image image, double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            RemoveGpsFromExif(image.Metadata.ExifProfile);
            return;
        }

        image.Metadata.ExifProfile ??= new ExifProfile();
        ExifProfile exif = image.Metadata.ExifProfile;
        RemoveGpsFromExif(exif);

        double lat = Math.Clamp(latitude.Value, -90d, 90d);
        double lon = Math.Clamp(longitude.Value, -180d, 180d);

        exif.SetValue(ExifTag.GPSLatitudeRef, lat >= 0 ? "N" : "S");
        exif.SetValue(ExifTag.GPSLatitude, ToExifDms(Math.Abs(lat)));
        exif.SetValue(ExifTag.GPSLongitudeRef, lon >= 0 ? "E" : "W");
        exif.SetValue(ExifTag.GPSLongitude, ToExifDms(Math.Abs(lon)));
    }

    private static Rational[] ToExifDms(double absoluteCoordinate)
    {
        uint degrees = (uint)Math.Floor(absoluteCoordinate);
        double minutesFull = (absoluteCoordinate - degrees) * 60.0;
        uint minutes = (uint)Math.Floor(minutesFull);
        double seconds = (minutesFull - minutes) * 60.0;
        uint secondsScaled = (uint)Math.Round(seconds * 10000.0);

        return
        [
            new Rational(degrees, 1),
            new Rational(minutes, 1),
            new Rational(secondsScaled, 10000)
        ];
    }

    private static (double? Latitude, double? Longitude) ExtractGpsCoordinates(ExifProfile exif)
    {
        if (!exif.TryGetValue(ExifTag.GPSLatitude, out IExifValue<Rational[]>? latVal) ||
            !exif.TryGetValue(ExifTag.GPSLongitude, out IExifValue<Rational[]>? lonVal) ||
            latVal.Value is not { Length: >= 3 } latParts ||
            lonVal.Value is not { Length: >= 3 } lonParts)
        {
            return (null, null);
        }

        double? lat = DmsToDecimal(latParts);
        double? lon = DmsToDecimal(lonParts);

        if (!lat.HasValue || !lon.HasValue)
            return (null, null);

        string? latRef = GetExifString(exif, ExifTag.GPSLatitudeRef);
        string? lonRef = GetExifString(exif, ExifTag.GPSLongitudeRef);

        if (string.Equals(latRef, "S", StringComparison.OrdinalIgnoreCase))
            lat = -Math.Abs(lat.Value);
        if (string.Equals(lonRef, "W", StringComparison.OrdinalIgnoreCase))
            lon = -Math.Abs(lon.Value);

        return (Math.Round(lat.Value, 6), Math.Round(lon.Value, 6));
    }

    private static double? DmsToDecimal(Rational[] parts)
    {
        if (parts[0].Denominator == 0 || parts[1].Denominator == 0 || parts[2].Denominator == 0)
            return null;

        double degrees = (double)parts[0].Numerator / parts[0].Denominator;
        double minutes = (double)parts[1].Numerator / parts[1].Denominator;
        double seconds = (double)parts[2].Numerator / parts[2].Denominator;

        double result = degrees + (minutes / 60.0) + (seconds / 3600.0);
        return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
    }

    private static string? GetExifString(ExifProfile exif, ExifTag<string> tag)
    {
        return exif.TryGetValue(tag, out IExifValue<string>? val) ? val.Value : null;
    }

    private static string? CleanExifString(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string cleaned = input.Replace("\0", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        return cleaned.Length > maxLength ? cleaned[..maxLength].Trim() : cleaned;
    }

    private static string? FormatFocalLength(ExifProfile exif)
    {
        if (exif.TryGetValue(ExifTag.FocalLength, out IExifValue<Rational>? focal) &&
            focal.Value.Denominator != 0)
        {
            double mm = (double)focal.Value.Numerator / focal.Value.Denominator;
            if (mm > 0)
                return $"{mm.ToString("0.#", CultureInfo.InvariantCulture)} mm";
        }

        if (exif.TryGetValue(ExifTag.FocalLengthIn35mmFilm, out IExifValue<ushort>? focal35) &&
            focal35.Value > 0)
        {
            return $"{focal35.Value} mm";
        }

        return null;
    }

    private static string? FormatAperture(ExifProfile exif)
    {
        if (exif.TryGetValue(ExifTag.FNumber, out IExifValue<Rational>? fNumber) &&
            fNumber.Value.Denominator != 0)
        {
            double val = (double)fNumber.Value.Numerator / fNumber.Value.Denominator;
            if (val > 0)
                return $"f/{val.ToString("0.#", CultureInfo.InvariantCulture)}";
        }

        return null;
    }

    private static string? FormatShutterSpeed(ExifProfile exif)
    {
        if (!exif.TryGetValue(ExifTag.ExposureTime, out IExifValue<Rational>? exposure) ||
            exposure.Value.Denominator == 0 ||
            exposure.Value.Numerator == 0)
        {
            return null;
        }

        uint num = exposure.Value.Numerator;
        uint den = exposure.Value.Denominator;
        double seconds = (double)num / den;

        if (seconds <= 0)
            return null;

        if (seconds >= 1.0)
            return $"{seconds.ToString("0.##", CultureInfo.InvariantCulture)} s";

        long reciprocal = (long)Math.Round(1.0 / seconds);
        return reciprocal > 1 ? $"1/{reciprocal} s" : $"{seconds.ToString("0.###", CultureInfo.InvariantCulture)} s";
    }

    private static int? ExtractIso(ExifProfile exif)
    {
        if (exif.TryGetValue(ExifTag.ISOSpeedRatings, out IExifValue<ushort[]>? isoRatings) &&
            isoRatings.Value is { Length: > 0 } arr &&
            arr[0] > 0)
        {
            return arr[0];
        }

        if (exif.TryGetValue(ExifTag.RecommendedExposureIndex, out IExifValue<uint>? rei) &&
            rei.Value is > 0 and <= int.MaxValue)
        {
            return (int)rei.Value;
        }

        return null;
    }

    private static Instant? ExtractTakenAt(ExifProfile exif)
    {
        string? rawDate = GetExifString(exif, ExifTag.DateTimeOriginal)
                          ?? GetExifString(exif, ExifTag.DateTimeDigitized)
                          ?? GetExifString(exif, ExifTag.DateTime);

        if (string.IsNullOrWhiteSpace(rawDate))
            return null;

        string cleaned = rawDate.Replace("\0", string.Empty).Trim();
        string[] formats =
        [
            "yyyy:MM:dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy:MM:dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss"
        ];

        if (DateTime.TryParseExact(cleaned, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime parsed))
        {
            return Instant.FromDateTimeUtc(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
        }

        return null;
    }

    private static string NormalizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return ".jpg";

        return ext.ToLowerInvariant();
    }
}
