using NpgsqlTypes;

namespace Catalog.Api.Data;

/// <summary>Lifecycle stage of a Product. Any transition is allowed.</summary>
public enum ProductStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>An item of the catalog (see GLOSSARY.md). It has no price.</summary>
public sealed class Product : ITenantScoped
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 5000;
    public const int SkuMaxLength = 64;
    public const int SlugMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Sku { get; set; }
    public required string Slug { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    /// <summary>Attribute values keyed by Attribute Definition id, stored as jsonb (ADR 0006).</summary>
    public string Attributes { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
    public DateTimeOffset UpdatedAt { get; set; } = Timestamps.UtcNow();

    /// <summary>Stored generated full-text vector (Spanish): name weighted A, description B (ADR 0006).</summary>
    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}
