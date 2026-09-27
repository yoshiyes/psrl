using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;

namespace Passerelle.Host.Repositories;

public sealed class LinkRepository(ApplicationDbContext db) : ILinkRepository
{
    private const string TextSearchConfig = "unaccent_simple";

    public async Task<(IReadOnlyList<Link> Items, int TotalCount)> SearchAsync(string? query, IReadOnlyCollection<string>? categories,
        string sort, int page, int pageSize)
    {
        var sql = db.Links
            .AsNoTracking()
            .Include(x => x.LinkCategories)
            .ThenInclude(x => x.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            string cleanQuery = query.Trim();
            string term = cleanQuery.ToLower();
            string? prefixQuery = !cleanQuery.Contains('"') ? FormatPrefixQuery(cleanQuery) : null;

            if (prefixQuery is not null)
            {
                sql = sql.Where(x =>
                    x.SearchVector.Matches(EF.Functions.ToTsQuery(TextSearchConfig, prefixQuery)) ||
                    x.Url.ToLower().Contains(term));

                sql = sort switch
                {
                    "relevance" => sql.OrderByDescending(x => x.SearchVector.Rank(EF.Functions.ToTsQuery(TextSearchConfig, prefixQuery)))
                        .ThenByDescending(x => x.CreatedAt),
                    "title_asc" => sql.OrderBy(x => x.Title),
                    "title_desc" => sql.OrderByDescending(x => x.Title),
                    "description_asc" => sql.OrderBy(x => x.Description),
                    "description_desc" => sql.OrderByDescending(x => x.Description),
                    "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                    _ => sql.OrderByDescending(x => x.SearchVector.Rank(EF.Functions.ToTsQuery(TextSearchConfig, prefixQuery)))
                        .ThenByDescending(x => x.CreatedAt)
                };
            }
            else
            {
                sql = sql.Where(x =>
                    x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery(TextSearchConfig, cleanQuery)) ||
                    x.Url.ToLower().Contains(term));

                sql = sort switch
                {
                    "relevance" => sql.OrderByDescending(x => x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery(TextSearchConfig, cleanQuery)))
                        .ThenByDescending(x => x.CreatedAt),
                    "title_asc" => sql.OrderBy(x => x.Title),
                    "title_desc" => sql.OrderByDescending(x => x.Title),
                    "description_asc" => sql.OrderBy(x => x.Description),
                    "description_desc" => sql.OrderByDescending(x => x.Description),
                    "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                    _ => sql.OrderByDescending(x => x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery(TextSearchConfig, cleanQuery)))
                        .ThenByDescending(x => x.CreatedAt)
                };
            }
        }
        else
        {
            sql = sort switch
            {
                "title_asc" => sql.OrderBy(x => x.Title),
                "title_desc" => sql.OrderByDescending(x => x.Title),
                "description_asc" => sql.OrderBy(x => x.Description),
                "description_desc" => sql.OrderByDescending(x => x.Description),
                "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                _ => sql.OrderByDescending(x => x.CreatedAt)
            };
        }

        if (categories is { Count: > 0 })
            sql = sql.Where(x => x.LinkCategories.Any(c => categories.Contains(c.Category.Slug)));

        int total = await sql.CountAsync();
        var items = await sql.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        return (items, total);
    }

    public async Task<Link?> GetByIdAsync(Guid id)
    {
        return await db.Links
            .Include(x => x.LinkCategories)
            .ThenInclude(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Link link, IReadOnlyCollection<Guid> categoryIds)
    {
        foreach (Guid categoryId in categoryIds.Distinct())
            link.LinkCategories.Add(new LinkCategory { CategoryId = categoryId, Link = link });

        db.Links.Add(link);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Link link, IReadOnlyCollection<Guid> categoryIds)
    {
        Link existing = await db.Links.Include(x => x.LinkCategories).FirstAsync(x => x.Id == link.Id);
        existing.Title = link.Title;
        existing.Url = link.Url;
        existing.Description = link.Description;
        existing.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        db.LinkCategories.RemoveRange(existing.LinkCategories);
        existing.LinkCategories = categoryIds.Distinct()
            .Select(categoryId => new LinkCategory { LinkId = existing.Id, CategoryId = categoryId })
            .ToList();

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        Link? link = await db.Links.FirstOrDefaultAsync(x => x.Id == id);
        
        if (link is null) return;
        
        db.Links.Remove(link);
        await db.SaveChangesAsync();
    }

    private static string? FormatPrefixQuery(string query)
    {
        MatchCollection matches = Regex.Matches(query, @"[\w]+");
        
        if (matches.Count == 0) return null;

        var tokens = matches
            .Select(m => m.Value.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => $"{t}:*");

        return string.Join(" & ", tokens);
    }
}
