using NodaTime;

namespace Passerelle.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}