using System.ComponentModel.DataAnnotations;
using Passerelle.Domain.Common;

namespace Passerelle.Domain.Entities;

public class ImageTag : AuditableEntity
{
    [Required] [StringLength(80)] public required string Name { get; set; }

    [Required] [StringLength(120)] public required string Slug { get; set; }

    [StringLength(32)] public string? Emoji { get; set; }

    public virtual ICollection<GalleryItemTag> GalleryItemTags { get; set; } = [];
}
