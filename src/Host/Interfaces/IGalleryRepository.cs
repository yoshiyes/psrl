using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public interface IGalleryRepository
{
    Task<(IReadOnlyList<GalleryItem> Items, int TotalCount)> SearchAsync(
        string? query,
        GalleryItemType? type,
        IReadOnlyCollection<string>? tags,
        string sort,
        int page,
        int pageSize);

    Task<GalleryItem?> GetByIdAsync(Guid id);
    Task AddAsync(GalleryItem item, IReadOnlyCollection<Guid> tagIds);
    Task UpdateAsync(GalleryItem item, IReadOnlyCollection<Guid> tagIds);
    Task DeleteAsync(Guid id);
}
