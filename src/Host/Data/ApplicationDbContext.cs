using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Passerelle.Domain.Common;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.Data;

public class ApplicationDbContext(DbContextOptions options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Link> Links => Set<Link>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<LinkCategory> LinkCategories => Set<LinkCategory>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
    public DbSet<ImageTag> ImageTags => Set<ImageTag>();
    public DbSet<GalleryItemTag> GalleryItemTags => Set<GalleryItemTag>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region Category

        modelBuilder.Entity<Category>()
            .HasKey(c => c.Id);
        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();
        modelBuilder.Entity<Category>()
            .Property(c => c.Name)
            .HasMaxLength(80);
        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Slug)
            .IsUnique();
        modelBuilder.Entity<Category>()
            .Property(c => c.Slug)
            .HasMaxLength(120);
        modelBuilder.Entity<Category>()
            .Property(c => c.Emoji)
            .HasMaxLength(32);

        #endregion

        #region LinkCategory

        modelBuilder.Entity<LinkCategory>()
            .HasKey(x => new { x.LinkId, x.CategoryId });

        modelBuilder.Entity<LinkCategory>()
            .HasOne(x => x.Link)
            .WithMany(x => x.LinkCategories)
            .HasForeignKey(x => x.LinkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LinkCategory>()
            .HasOne(x => x.Category)
            .WithMany(x => x.LinkCategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Link

        modelBuilder.Entity<Link>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Link>()
            .Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(160);

        modelBuilder.Entity<Link>()
            .Property(c => c.Url)
            .IsRequired()
            .HasMaxLength(128);

        modelBuilder.Entity<Link>()
            .Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(1200);

        #endregion

        #region ImageTag

        modelBuilder.Entity<ImageTag>()
            .HasKey(t => t.Id);
        modelBuilder.Entity<ImageTag>()
            .HasIndex(t => t.Name)
            .IsUnique();
        modelBuilder.Entity<ImageTag>()
            .Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(80);
        modelBuilder.Entity<ImageTag>()
            .HasIndex(t => t.Slug)
            .IsUnique();
        modelBuilder.Entity<ImageTag>()
            .Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(120);
        modelBuilder.Entity<ImageTag>()
            .Property(t => t.Emoji)
            .HasMaxLength(32);

        #endregion

        #region GalleryItemTag

        modelBuilder.Entity<GalleryItemTag>()
            .HasKey(x => new { x.GalleryItemId, x.ImageTagId });

        modelBuilder.Entity<GalleryItemTag>()
            .HasOne(x => x.GalleryItem)
            .WithMany(x => x.GalleryItemTags)
            .HasForeignKey(x => x.GalleryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GalleryItemTag>()
            .HasOne(x => x.ImageTag)
            .WithMany(x => x.GalleryItemTags)
            .HasForeignKey(x => x.ImageTagId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region GalleryItem

        modelBuilder.Entity<GalleryItem>()
            .HasKey(g => g.Id);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.Title)
            .HasMaxLength(160);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.Description)
            .HasMaxLength(1200);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.StoredFileName)
            .IsRequired()
            .HasMaxLength(120);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.ThumbnailFileName)
            .IsRequired()
            .HasMaxLength(120);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.PreviewFileName)
            .IsRequired()
            .HasMaxLength(120);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.CameraMake)
            .HasMaxLength(100);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.CameraModel)
            .HasMaxLength(100);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.LensModel)
            .HasMaxLength(160);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.FocalLength)
            .HasMaxLength(40);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.Aperture)
            .HasMaxLength(40);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.ShutterSpeed)
            .HasMaxLength(40);

        modelBuilder.Entity<GalleryItem>()
            .Property(g => g.LocationName)
            .HasMaxLength(160);

        #endregion

        #region SiteSettings

        modelBuilder.Entity<SiteSettings>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<SiteSettings>()
            .Property(s => s.IsGalleryEnabled)
            .HasDefaultValue(true);

        #endregion
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = SystemClock.Instance.GetCurrentInstant();
                    entry.Entity.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
                    break;
            }
    }
}
