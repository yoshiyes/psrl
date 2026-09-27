using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();

    Task<(IReadOnlyList<Category> Items, int TotalCount)> SearchAsync(string? query, string sort, int page,
        int pageSize);

    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> HasLinkedItemsAsync(Guid id);
    Task DeleteAsync(Guid id);
}