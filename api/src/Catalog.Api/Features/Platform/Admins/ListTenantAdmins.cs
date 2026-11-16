using Catalog.Api.Data;
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

    private static async Task<Results<Ok<List<AdminInvitation.AdminResponse>>, NotFound>> Handle(
        Guid id, CatalogDbContext db, CancellationToken ct)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == id, ct))
        {
            return TypedResults.NotFound();
        }

        var admins = await db.AdminMemberships
            .Where(m => m.TenantId == id)
            .OrderBy(m => m.Email)
            .Select(m => new AdminInvitation.AdminResponse(m.Id, m.UserId, m.Email))
            .ToListAsync(ct);

        return TypedResults.Ok(admins);
    }
}
