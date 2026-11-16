namespace Catalog.Api.Data;

/// <summary>
/// An entity that belongs to one Tenant. CatalogDbContext applies a global query filter on
/// TenantId and fills it in on insert (ADR 0003).
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
