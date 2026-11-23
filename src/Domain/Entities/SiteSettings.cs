using Passerelle.Domain.Common;

namespace Passerelle.Domain.Entities;

public class SiteSettings : AuditableEntity
{
    public bool IsGalleryEnabled { get; set; } = true;
}
