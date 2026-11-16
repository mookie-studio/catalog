using Catalog.Api.Data;
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

    private static async Task<Ok<List<AdminInvitation.AdminResponse>>> Handle(
        ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        var admins = await db.AdminMemberships
            .Where(m => m.TenantId == currentTenant.TenantId)
            .OrderBy(m => m.Email)
            .Select(m => new AdminInvitation.AdminResponse(m.Id, m.UserId, m.Email))
            .ToListAsync(ct);

        return TypedResults.Ok(admins);
    }
}
