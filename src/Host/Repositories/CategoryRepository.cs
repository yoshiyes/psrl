using Microsoft.EntityFrameworkCore;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;
using SystemClock = NodaTime.SystemClock;

namespace Passerelle.Host.Repositories;

public sealed class CategoryRepository(ApplicationDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<(IReadOnlyList<Category> Items, int TotalCount)> SearchAsync(string? query, string sort,
        int page, int pageSize)
    {
        var sql = db.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            string term = query.Trim().ToLower();
            sql = sql.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.Slug.ToLower().Contains(term));
        }

        sql = sort switch
        {
            "name_desc" => sql.OrderByDescending(x => x.Name),
            _ => sql.OrderBy(x => x.Name)
        };

        int total = await sql.CountAsync();
        var items = await sql.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await db.Categories.FindAsync(id);
    }

    public async Task AddAsync(Category category)
    {
        category.Slug = BuildSlug(category.Name);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        Category existing = await db.Categories.FirstAsync(x => x.Id == category.Id);
        existing.Name = category.Name;
        existing.Slug = BuildSlug(category.Name);
        existing.Emoji = category.Emoji;
        existing.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasLinkedItemsAsync(Guid id)
    {
        return await db.LinkCategories.AnyAsync(x => x.CategoryId == id);
    }

    public async Task DeleteAsync(Guid id)
    {
        Category? category = await db.Categories.FindAsync(id);
        
        if (category is null) return;
        
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }

    private static string BuildSlug(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(" ", "-");
    }
}