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
