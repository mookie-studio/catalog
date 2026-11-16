using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Catalog.Api.Features.Platform.Tenants;

/// <summary>Deactivates or reactivates a Tenant. Both operations are idempotent; Tenants are never deleted.</summary>
public static class ChangeTenantStatus
{
    public static RouteGroupBuilder MapChangeTenantStatus(this RouteGroupBuilder platform)
    {
        platform.MapPost("/tenants/{id:guid}/deactivate",
            (Guid id, CatalogDbContext db, CancellationToken ct) => SetActive(id, false, db, ct))
            .WithName("DeactivateTenant");
        platform.MapPost("/tenants/{id:guid}/reactivate",
            (Guid id, CatalogDbContext db, CancellationToken ct) => SetActive(id, true, db, ct))
            .WithName("ReactivateTenant");
        return platform;
    }

    private static async Task<Results<NoContent, NotFound>> SetActive(
        Guid id, bool isActive, CatalogDbContext db, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([id], ct);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        tenant.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
