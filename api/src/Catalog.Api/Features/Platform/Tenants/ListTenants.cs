using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Platform.Tenants;

public static class ListTenants
{
    public static RouteGroupBuilder MapListTenants(this RouteGroupBuilder platform)
    {
        platform.MapGet("/tenants", Handle).WithName("ListTenants");
        return platform;
    }

    private static async Task<Ok<PagedResponse<TenantResponse>>> Handle(
        int? page, int? pageSize, CatalogDbContext db, CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var totalCount = await db.Tenants.CountAsync(ct);
        var items = await db.Tenants
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(t => new TenantResponse(t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<TenantResponse>(items, paging.Page, paging.PageSize, totalCount));
    }
}
