using Passerelle.Domain.Entities;

namespace Passerelle.Host.ViewModels;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<Link> Links { get; init; } = [];
    public IReadOnlyList<Category> Categories { get; init; } = [];
    public string? Query { get; init; }
    public IReadOnlyList<string> SelectedCategories { get; init; } = [];
    public string Sort { get; init; } = "newest";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}