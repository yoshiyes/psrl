using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class ImageTagsIndexViewModel
{
    public IReadOnlyList<ImageTag> Tags { get; init; } = [];
    public string? Query { get; init; }
    public string Sort { get; init; } = "name_asc";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
