using Passerelle.Domain.Entities;

namespace Passerelle.Host.Interfaces;

public interface ISiteSettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<bool> IsGalleryEnabledAsync(CancellationToken cancellationToken = default);
    Task UpdateGalleryEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}
