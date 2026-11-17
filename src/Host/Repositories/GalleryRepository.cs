using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;

namespace Passerelle.Host.Repositories;

public sealed class GalleryRepository(ApplicationDbContext db) : IGalleryRepository
{
    private const string TextSearchConfig = "unaccent_simple";

    public async Task<(IReadOnlyList<GalleryItem> Items, int TotalCount)> SearchAsync(
        string? query,
        GalleryItemType? type,
        IReadOnlyCollection<string>? tags,
        string sort,
        int page,
        int pageSize)
    {
        var sql = db.GalleryItems
            .AsNoTracking()
            .Include(x => x.GalleryItemTags)
            .ThenInclude(x => x.ImageTag)
            .AsQueryable();

        if (type.HasValue)
            sql = sql.Where(x => x.Type == type.Value);

        if (tags is { Count: > 0 })
            sql = sql.Where(x => x.GalleryItemTags.Any(t => tags.Contains(t.ImageTag.Slug)));

        if (!string.IsNullOrWhiteSpace(query))
        {
            string cleanQuery = query.Trim();
            string term = cleanQuery.ToLower();

            if (db.Database.IsSqlite())
            {
                sql = sql.Where(x =>
                    x.Title.ToLower().Contains(term) ||
                    x.Description.ToLower().Contains(term) ||
                    (x.LocationName != null && x.LocationName.ToLower().Contains(term)) ||
                    (x.CameraMake != null && x.CameraMake.ToLower().Contains(term)) ||
                    (x.CameraModel != null && x.CameraModel.ToLower().Contains(term)));

                sql = sort switch
                {
                    "title_asc" => sql.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt),
                    "title_desc" => sql.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt),
                    "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                    _ => sql.OrderByDescending(x => x.CreatedAt)
                };
            }
            else // PostgreSQL
            {
                string? prefixQuery = !cleanQuery.Contains('"') ? FormatPrefixQuery(cleanQuery) : null;
                string tsQueryInput = prefixQuery ?? cleanQuery;

                sql = sql.Where(x =>
                    x.SearchVector!.Matches(EF.Functions.ToTsQuery(TextSearchConfig, tsQueryInput)) ||
                    x.Title.ToLower().Contains(term) ||
                    x.Description.ToLower().Contains(term));

                sql = sort switch
                {
                    "relevance" => sql.OrderByDescending(x =>
                            x.SearchVector!.Rank(EF.Functions.ToTsQuery(TextSearchConfig, tsQueryInput)))
                        .ThenByDescending(x => x.CreatedAt),
                    "title_asc" => sql.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt),
                    "title_desc" => sql.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt),
                    "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                    _ => sql.OrderByDescending(x =>
                            x.SearchVector!.Rank(EF.Functions.ToTsQuery(TextSearchConfig, tsQueryInput)))
                        .ThenByDescending(x => x.CreatedAt)
                };
            }
        }
        else
        {
            sql = sort switch
            {
                "title_asc" => sql.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt),
                "title_desc" => sql.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt),
                "date_asc" or "oldest" => sql.OrderBy(x => x.CreatedAt),
                _ => sql.OrderByDescending(x => x.CreatedAt)
            };
        }

        int total = await sql.CountAsync();
        var items = await sql.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<GalleryItem?> GetByIdAsync(Guid id)
    {
        return await db.GalleryItems
            .Include(x => x.GalleryItemTags)
            .ThenInclude(x => x.ImageTag)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(GalleryItem item, IReadOnlyCollection<Guid> tagIds)
    {
        Instant now = SystemClock.Instance.GetCurrentInstant();
        foreach (Guid tagId in tagIds.Distinct())
        {
            item.GalleryItemTags.Add(new GalleryItemTag
            {
                ImageTagId = tagId,
                GalleryItem = item,
                CreatedAt = now
            });
        }

        db.GalleryItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(GalleryItem item, IReadOnlyCollection<Guid> tagIds)
    {
        GalleryItem existing = await db.GalleryItems
            .Include(x => x.GalleryItemTags)
            .FirstAsync(x => x.Id == item.Id);

        existing.Title = item.Title;
        existing.Description = item.Description;
        existing.Type = item.Type;
        existing.OriginalFileName = item.OriginalFileName;
        existing.StoredFileName = item.StoredFileName;
        existing.ThumbnailFileName = item.ThumbnailFileName;
        existing.PreviewFileName = item.PreviewFileName;
        existing.ContentType = item.ContentType;
        existing.FileSizeBytes = item.FileSizeBytes;
        existing.Width = item.Width;
        existing.Height = item.Height;

        existing.CameraMake = item.CameraMake;
        existing.CameraModel = item.CameraModel;
        existing.LensModel = item.LensModel;
        existing.FocalLength = item.FocalLength;
        existing.Aperture = item.Aperture;
        existing.ShutterSpeed = item.ShutterSpeed;
        existing.Iso = item.Iso;
        existing.TakenAt = item.TakenAt;

        existing.LocationMode = item.LocationMode;
        existing.LocationName = item.LocationName;
        existing.Latitude = item.Latitude;
        existing.Longitude = item.Longitude;

        Instant now = SystemClock.Instance.GetCurrentInstant();
        existing.UpdatedAt = now;

        db.GalleryItemTags.RemoveRange(existing.GalleryItemTags);
        existing.GalleryItemTags = tagIds.Distinct()
            .Select(tagId => new GalleryItemTag
            {
                GalleryItemId = existing.Id,
                ImageTagId = tagId,
                CreatedAt = now
            })
            .ToList();

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        GalleryItem? item = await db.GalleryItems.FirstOrDefaultAsync(x => x.Id == id);

        if (item is null) return;

        db.GalleryItems.Remove(item);
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
