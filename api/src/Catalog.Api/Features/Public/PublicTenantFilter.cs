using Catalog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Resolves the current Tenant of /api/public/{tenantSlug} routes from the Tenant slug.
/// Unknown and Deactivated tenants are 404, so nothing about them leaks.
/// </summary>
public sealed class PublicTenantFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var slug = http.Request.RouteValues["tenantSlug"] as string;
        var db = http.RequestServices.GetRequiredService<CatalogDbContext>();
        var tenantId = await db.Tenants
            .Where(t => t.Slug == slug && t.IsActive)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync(http.RequestAborted);

        if (tenantId is null)
        {
            return TypedResults.NotFound();
        }

        http.RequestServices.GetRequiredService<ICurrentTenant>().Set(tenantId.Value);
        return await next(context);
    }
}
