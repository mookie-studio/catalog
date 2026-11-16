namespace Catalog.Api.Data;

/// <summary>An optional grouping of Products, organized as a tree; Admins order siblings by Position.</summary>
public sealed class Category : ITenantScoped
{
    public const int NameMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
    public int Position { get; set; }
}

/// <summary>Links a Product to a Category. Tenant-scoped so direct queries are filtered too.</summary>
public sealed class ProductCategory : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }
}
