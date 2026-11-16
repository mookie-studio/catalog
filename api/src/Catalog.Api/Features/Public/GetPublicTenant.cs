using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

public static class GetPublicTenant
{
    public sealed record Response(string Name, string Slug);

    public static RouteGroupBuilder MapGetPublicTenant(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("", Handle).WithName("GetPublicTenant");
        return publicGroup;
    }

    private static async Task<Ok<Response>> Handle(ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .Where(t => t.Id == currentTenant.TenantId)
            .Select(t => new Response(t.Name, t.Slug))
            .SingleAsync(ct);
        return TypedResults.Ok(tenant);
    }
}
