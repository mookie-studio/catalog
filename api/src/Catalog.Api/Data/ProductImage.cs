namespace Catalog.Api.Data;

/// <summary>A picture of a Product. Images are ordered; the first is the Main image.</summary>
public sealed class ProductImage : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; init; }
    public required string StorageKey { get; init; }
    public required string ContentType { get; init; }

    /// <summary>Storage key of the Thumbnail (ADR 0008); null for images stored before Thumbnails existed.</summary>
    public string? ThumbnailKey { get; set; }
    public int Position { get; set; }
}
