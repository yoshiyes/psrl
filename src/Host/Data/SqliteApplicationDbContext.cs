using Microsoft.EntityFrameworkCore;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.Data;

public sealed class SqliteApplicationDbContext(DbContextOptions<SqliteApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Link>()
            .Ignore(c => c.SearchVector);
    }
}
