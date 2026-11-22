using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public interface IImageTagRepository
{
    Task<IReadOnlyList<ImageTag>> GetAllAsync();

    Task<(IReadOnlyList<ImageTag> Items, int TotalCount)> SearchAsync(string? query, string sort, int page,
        int pageSize);

    Task<ImageTag?> GetByIdAsync(Guid id);
    Task AddAsync(ImageTag tag);
    Task UpdateAsync(ImageTag tag);
    Task<bool> HasLinkedItemsAsync(Guid id);
    Task DeleteAsync(Guid id);

    Task<IReadOnlyCollection<Guid>> ResolveTagIdsAsync(IReadOnlyCollection<Guid> selectedTagIds, string? newTagsInput);
}
