using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.Property(t => t.Name).HasMaxLength(Tenant.NameMaxLength).IsRequired();
            tenant.Property(t => t.Slug).HasMaxLength(Tenant.SlugMaxLength).IsRequired();
            tenant.Property(t => t.IsActive).HasDefaultValue(true);
            tenant.HasIndex(t => t.Slug).IsUnique();
        });
    }
}
