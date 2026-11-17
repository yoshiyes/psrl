using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class GalleryIndexViewModel
{
    public IReadOnlyList<GalleryItem> Items { get; init; } = [];
    public IReadOnlyList<ImageTag> Tags { get; init; } = [];
    public string? Query { get; init; }
    public string? SelectedType { get; init; }
    public IReadOnlyList<string> SelectedTags { get; init; } = [];
    public string Sort { get; init; } = "newest";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
