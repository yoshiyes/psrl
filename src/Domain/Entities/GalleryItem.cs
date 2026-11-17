using System.ComponentModel.DataAnnotations;
using NodaTime;
using NpgsqlTypes;
using Passerelle.Domain.Common;

namespace Passerelle.Domain.Entities;

public class GalleryItem : AuditableEntity
{
    [StringLength(160)] public string Title { get; set; } = string.Empty;

    [StringLength(1200)] public string Description { get; set; } = string.Empty;

    public GalleryItemType Type { get; set; } = GalleryItemType.Image;

    [Required] [StringLength(260)] public string OriginalFileName { get; set; } = string.Empty;

    [Required] [StringLength(120)] public string StoredFileName { get; set; } = string.Empty;

    [Required] [StringLength(120)] public string ThumbnailFileName { get; set; } = string.Empty;

    [Required] [StringLength(120)] public string PreviewFileName { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    // Photo EXIF metadata
    [StringLength(100)] public string? CameraMake { get; set; }

    [StringLength(100)] public string? CameraModel { get; set; }

    [StringLength(160)] public string? LensModel { get; set; }

    [StringLength(40)] public string? FocalLength { get; set; }

    [StringLength(40)] public string? Aperture { get; set; }

    [StringLength(40)] public string? ShutterSpeed { get; set; }

    public int? Iso { get; set; }

    public Instant? TakenAt { get; set; }

    // Photo Location metadata
    public GalleryLocationMode LocationMode { get; set; } = GalleryLocationMode.Original;

    [StringLength(160)] public string? LocationName { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public NpgsqlTsVector? SearchVector { get; set; }

    public virtual ICollection<GalleryItemTag> GalleryItemTags { get; set; } = [];
}
