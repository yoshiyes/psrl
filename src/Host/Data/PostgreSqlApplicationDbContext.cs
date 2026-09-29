using Microsoft.EntityFrameworkCore;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.Data;

public sealed class PostgreSqlApplicationDbContext(DbContextOptions<PostgreSqlApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Link>()
            .Property(c => c.SearchVector)
            .HasComputedColumnSql(
                "setweight(to_tsvector('unaccent_simple', coalesce(\"Title\", '')), 'A') || " +
                "setweight(to_tsvector('unaccent_simple', coalesce(\"Description\", '')), 'B') || " +
                "setweight(to_tsvector('unaccent_simple', coalesce(\"Url\", '') || ' ' || regexp_replace(coalesce(\"Url\", ''), '[^a-zA-Z0-9]+', ' ', 'g')), 'C')",
                stored: true);

        modelBuilder.Entity<Link>()
            .HasIndex(c => c.SearchVector)
            .HasMethod("GIN");
    }
}
