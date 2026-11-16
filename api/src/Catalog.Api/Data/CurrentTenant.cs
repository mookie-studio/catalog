namespace Catalog.Api.Data;

/// <summary>The Tenant of the current request: from the Admin's membership, or from the public Tenant slug.</summary>
public interface ICurrentTenant
{
    Guid? TenantId { get; }

    void Set(Guid tenantId);
}

public sealed class CurrentTenant : ICurrentTenant
{
    public Guid? TenantId { get; private set; }

    public void Set(Guid tenantId)
    {
        if (TenantId is { } existing && existing != tenantId)
        {
            throw new InvalidOperationException("The current Tenant cannot change during a request.");
        }

        TenantId = tenantId;
    }
}
