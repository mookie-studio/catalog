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

    /// <summary>Maps to PostgreSQL's built-in jsonb_extract_path_text(jsonb, key); only usable in queries.</summary>
    public static string? JsonbExtractPathText(string json, string key) =>
        throw new InvalidOperationException("Only translatable to SQL.");

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AdminMembership> AdminMemberships => Set<AdminMembership>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

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

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
            product.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
            product.Property(p => p.Sku).HasMaxLength(Product.SkuMaxLength).IsRequired();
            product.Property(p => p.Slug).HasMaxLength(Product.SlugMaxLength).IsRequired();
            product.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            product.Property(p => p.Attributes).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
            product.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
            product.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
            product.HasIndex(p => new { p.TenantId, p.Slug }).IsUnique();
            product.HasIndex(p => p.Attributes).HasMethod("gin");
            product.Property(p => p.SearchVector)
                .HasComputedColumnSql(
                    "setweight(to_tsvector('spanish'::regconfig, coalesce(\"Name\", '')), 'A') || " +
                    "setweight(to_tsvector('spanish'::regconfig, coalesce(\"Description\", '')), 'B')",
                    stored: true);
            product.HasIndex(p => p.SearchVector).HasMethod("gin");
        });

        modelBuilder.Entity<AttributeDefinition>(definition =>
        {
            definition.Property(d => d.Name).HasMaxLength(AttributeDefinition.NameMaxLength).IsRequired();
            definition.Property(d => d.NormalizedName).HasMaxLength(AttributeDefinition.NameMaxLength).IsRequired();
            definition.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
            definition.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
            definition.HasIndex(d => new { d.TenantId, d.NormalizedName }).IsUnique();
            definition.HasMany(d => d.Options).WithOne()
                .HasForeignKey(o => o.AttributeDefinitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttributeOption>(option =>
        {
            option.Property(o => o.Value).HasMaxLength(AttributeOption.ValueMaxLength).IsRequired();
            option.Property(o => o.NormalizedValue).HasMaxLength(AttributeOption.ValueMaxLength).IsRequired();
            option.HasIndex(o => new { o.AttributeDefinitionId, o.NormalizedValue }).IsUnique();
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
            category.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
            category.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
            category.HasIndex(c => new { c.TenantId, c.ParentId, c.Position });
        });

        modelBuilder.Entity<ProductCategory>(link =>
        {
            link.HasKey(l => new { l.ProductId, l.CategoryId });
            link.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Cascade);
            link.HasOne<Category>().WithMany().HasForeignKey(l => l.CategoryId).OnDelete(DeleteBehavior.Cascade);
            link.HasIndex(l => l.CategoryId);
        });

        modelBuilder.Entity<ProductImage>(image =>
        {
            image.Property(i => i.StorageKey).HasMaxLength(200).IsRequired();
            image.Property(i => i.ContentType).HasMaxLength(50).IsRequired();
            image.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
            image.HasIndex(i => new { i.ProductId, i.Position });
        });

        modelBuilder.HasDbFunction(typeof(CatalogDbContext).GetMethod(nameof(JsonbExtractPathText))!)
            .HasName("jsonb_extract_path_text")
            .IsBuiltIn();

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
