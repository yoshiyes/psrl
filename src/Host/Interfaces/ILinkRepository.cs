using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public interface ILinkRepository
{
    Task<(IReadOnlyList<Link> Items, int TotalCount)> SearchAsync(string? query, IReadOnlyCollection<string>? categories,
        string sort, int page, int pageSize);

    Task<Link?> GetByIdAsync(Guid id);
    Task AddAsync(Link link, IReadOnlyCollection<Guid> categoryIds);
    Task UpdateAsync(Link link, IReadOnlyCollection<Guid> categoryIds);
    Task DeleteAsync(Guid id);
}