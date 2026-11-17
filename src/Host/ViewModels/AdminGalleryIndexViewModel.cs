using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class AdminGalleryIndexViewModel
{
    public IReadOnlyList<GalleryItem> Items { get; init; } = [];
    public IReadOnlyList<ImageTag> Tags { get; init; } = [];
    public string? Query { get; init; }
    public string? SelectedType { get; init; }
    public string? SelectedTag { get; init; }
    public string Sort { get; init; } = "newest";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string? Message { get; init; }
}
