using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Platform.Admins;

public static class ListTenantAdmins
{
    public static RouteGroupBuilder MapListTenantAdmins(this RouteGroupBuilder platform)
    {
        platform.MapGet("/tenants/{id:guid}/admins", Handle).WithName("PlatformListAdmins");
        return platform;
    }

    private static async Task<Results<Ok<PagedResponse<AdminInvitation.AdminResponse>>, NotFound>> Handle(
        Guid id, int? page, int? pageSize, CatalogDbContext db, CancellationToken ct)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == id, ct))
        {
            return TypedResults.NotFound();
        }

        var paging = Paging.From(page, pageSize);
        var memberships = db.AdminMemberships.Where(m => m.TenantId == id);
        var totalCount = await memberships.CountAsync(ct);
        var admins = await memberships
            .OrderBy(m => m.Email).ThenBy(m => m.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(m => new AdminInvitation.AdminResponse(m.Id, m.UserId, m.Email))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<AdminInvitation.AdminResponse>(admins, paging.Page, paging.PageSize, totalCount));
    }
}
