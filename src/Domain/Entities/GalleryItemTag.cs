using NodaTime;

namespace Passerelle.Domain.Entities;

public class GalleryItemTag
{
    public Guid GalleryItemId { get; set; }
    public virtual GalleryItem GalleryItem { get; set; } = null!;

    public Guid ImageTagId { get; set; }
    public virtual ImageTag ImageTag { get; set; } = null!;

    public Instant CreatedAt { get; set; }
}
