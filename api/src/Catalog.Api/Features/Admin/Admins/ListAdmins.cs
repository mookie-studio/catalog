using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Admins;

public static class ListAdmins
{
    public static RouteGroupBuilder MapListAdmins(this RouteGroupBuilder admin)
    {
        admin.MapGet("/admins", Handle).WithName("ListAdmins");
        return admin;
    }

    private static async Task<Ok<PagedResponse<AdminInvitation.AdminResponse>>> Handle(
        int? page, int? pageSize, ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var memberships = db.AdminMemberships.Where(m => m.TenantId == currentTenant.TenantId);
        var totalCount = await memberships.CountAsync(ct);
        var admins = await memberships
            .OrderBy(m => m.Email).ThenBy(m => m.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(m => new AdminInvitation.AdminResponse(m.Id, m.UserId, m.Email))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<AdminInvitation.AdminResponse>(admins, paging.Page, paging.PageSize, totalCount));
    }
}
