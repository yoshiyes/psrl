using NodaTime;

namespace Passerelle.Domain.Entities;

public class LinkCategory
{
    public Guid LinkId { get; set; }
    public virtual Link Link { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public Instant CreatedAt { get; set; }
}