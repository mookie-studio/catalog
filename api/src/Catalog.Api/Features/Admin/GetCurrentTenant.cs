using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin;

public static class GetCurrentTenant
{
    public sealed record Response(Guid Id, string Name, string Slug);

    public static RouteGroupBuilder MapGetCurrentTenant(this RouteGroupBuilder admin)
    {
        admin.MapGet("/tenant", Handle).WithName("GetCurrentTenant");
        return admin;
    }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .Where(t => t.Id == currentTenant.TenantId)
            .Select(t => new Response(t.Id, t.Name, t.Slug))
            .SingleOrDefaultAsync(ct);

        return tenant is null ? TypedResults.NotFound() : TypedResults.Ok(tenant);
    }
}
