using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;
using Passerelle.Host.Validation;

namespace Passerelle.Host.Repositories;

public sealed class ImageTagRepository(ApplicationDbContext db) : IImageTagRepository
{
    private static readonly EmojiAttribute EmojiValidator = new();

    public async Task<IReadOnlyList<ImageTag>> GetAllAsync()
    {
        return await db.ImageTags.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<(IReadOnlyList<ImageTag> Items, int TotalCount)> SearchAsync(string? query, string sort,
        int page, int pageSize)
    {
        var sql = db.ImageTags.AsNoTracking().AsQueryable();

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

    public async Task<ImageTag?> GetByIdAsync(Guid id)
    {
        return await db.ImageTags.FindAsync(id);
    }

    public async Task AddAsync(ImageTag tag)
    {
        tag.Slug = BuildSlug(tag.Name);
        db.ImageTags.Add(tag);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ImageTag tag)
    {
        ImageTag existing = await db.ImageTags.FirstAsync(x => x.Id == tag.Id);
        existing.Name = tag.Name;
        existing.Slug = BuildSlug(tag.Name);
        existing.Emoji = tag.Emoji;
        existing.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        await db.SaveChangesAsync();
    }

    public async Task<bool> HasLinkedItemsAsync(Guid id)
    {
        return await db.GalleryItemTags.AnyAsync(x => x.ImageTagId == id);
    }

    public async Task DeleteAsync(Guid id)
    {
        ImageTag? tag = await db.ImageTags.FindAsync(id);

        if (tag is null) return;

        db.ImageTags.Remove(tag);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<Guid>> ResolveTagIdsAsync(IReadOnlyCollection<Guid> selectedTagIds,
        string? newTagsInput)
    {
        HashSet<Guid> resultIds = [..selectedTagIds];

        if (string.IsNullOrWhiteSpace(newTagsInput))
            return resultIds;

        string[] rawTokens = newTagsInput.Split([',', ';'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (rawTokens.Length == 0)
            return resultIds;

        List<ImageTag> existingTags = await db.ImageTags.ToListAsync();
        bool addedAny = false;

        foreach (string rawToken in rawTokens)
        {
            (string? emoji, string name) = ParseTagToken(rawToken);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name.Length > 80)
                name = name[..80].Trim();

            string slug = BuildSlug(name);
            if (string.IsNullOrWhiteSpace(slug))
                continue;

            ImageTag? match = existingTags.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                t.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                resultIds.Add(match.Id);
                continue;
            }

            var created = new ImageTag
            {
                Name = name,
                Slug = slug,
                Emoji = emoji
            };

            db.ImageTags.Add(created);
            existingTags.Add(created);
            resultIds.Add(created.Id);
            addedAny = true;
        }

        if (addedAny)
            await db.SaveChangesAsync();

        return resultIds;
    }

    private static (string? Emoji, string Name) ParseTagToken(string token)
    {
        string trimmed = token.Trim();
        int firstSpace = trimmed.IndexOf(' ');

        if (firstSpace > 0)
        {
            string candidateEmoji = trimmed[..firstSpace].Trim();
            string remainingName = trimmed[(firstSpace + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(remainingName) &&
                EmojiValidator.GetValidationResult(candidateEmoji, new ValidationContext(candidateEmoji)) ==
                ValidationResult.Success)
            {
                return (candidateEmoji, remainingName);
            }
        }

        return (null, trimmed);
    }

    private static string BuildSlug(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(" ", "-");
    }
}
