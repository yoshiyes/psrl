using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;

namespace Passerelle.Host.Services;

public sealed class SiteSettingsService(
    ApplicationDbContext db,
    IMemoryCache cache) : ISiteSettingsService
{
    private const string CacheKey = "Passerelle:SiteSettings";

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null)
            return cached;

        SiteSettings? settings = await db.SiteSettings
            .AsNoTracking()
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new SiteSettings { IsGalleryEnabled = true };
            db.SiteSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Set(CacheKey, settings, TimeSpan.FromMinutes(10));
        return settings;
    }

    public async Task<bool> IsGalleryEnabledAsync(CancellationToken cancellationToken = default)
    {
        SiteSettings settings = await GetAsync(cancellationToken);
        return settings.IsGalleryEnabled;
    }

    public async Task UpdateGalleryEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        SiteSettings? settings = await db.SiteSettings
            .OrderBy(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new SiteSettings { IsGalleryEnabled = enabled };
            db.SiteSettings.Add(settings);
        }
        else
        {
            settings.IsGalleryEnabled = enabled;
        }

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
    }
}
