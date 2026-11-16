using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options, ICurrentTenant currentTenant)
    : DbContext(options)
{
    /// <summary>
    /// Read by the global query filter on every query, so each context instance filters by its own request's Tenant.
    /// No current Tenant means Guid.Empty, which matches nothing.
    /// </summary>
    private Guid CurrentTenantId => currentTenant.TenantId ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AdminMembership> AdminMemberships => Set<AdminMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.Property(t => t.Name).HasMaxLength(Tenant.NameMaxLength).IsRequired();
            tenant.Property(t => t.Slug).HasMaxLength(Tenant.SlugMaxLength).IsRequired();
            tenant.Property(t => t.IsActive).HasDefaultValue(true);
            tenant.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<AdminMembership>(membership =>
        {
            membership.Property(m => m.Email).HasMaxLength(AdminMembership.EmailMaxLength).IsRequired();
            membership.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
            membership.HasIndex(m => new { m.TenantId, m.Email }).IsUnique();
            membership.HasIndex(m => m.UserId).IsUnique();
        });

        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>Applies "TenantId == current Tenant" to every ITenantScoped entity, by convention.</summary>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
            {
                continue;
            }

            var entity = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(entity, nameof(ITenantScoped.TenantId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId))),
                entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ITenantScoped.TenantId));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Sets TenantId on new tenant-scoped entities from the current Tenant.</summary>
    private void StampTenant()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = currentTenant.TenantId
                    ?? throw new InvalidOperationException("A tenant-scoped entity was added without a current Tenant.");
            }
        }
    }
}
