namespace Catalog.Api.Data;

/// <summary>
/// A previous Product slug (ADR 0009). It stays reserved for its product within the Tenant and
/// public requests that use it resolve to the product.
/// </summary>
public sealed class OldProductSlug : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; init; }
    public required string Slug { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
}

/// <summary>
/// A previous Tenant slug (ADR 0009). Unique across tenants and reserved for its tenant; public
/// requests that use it resolve to the tenant. Not tenant-scoped: only /api/platform and the
/// public tenant filter use it.
/// </summary>
public sealed class OldTenantSlug
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; init; }
    public required string Slug { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
}
