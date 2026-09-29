using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;
using Passerelle.Domain.Common;

namespace Passerelle.Domain.Entities;

public class Link : AuditableEntity
{
    [Required] [StringLength(160)] public string Title { get; set; } = string.Empty;

    [Required] [Url] [StringLength(2048)] public string Url { get; set; } = string.Empty;

    [Required] [StringLength(1200)] public string Description { get; set; } = string.Empty;

    public NpgsqlTsVector? SearchVector { get; set; }

    public virtual ICollection<LinkCategory> LinkCategories { get; set; } = [];
}
