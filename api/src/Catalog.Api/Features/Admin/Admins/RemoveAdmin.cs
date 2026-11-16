using System.Security.Claims;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Auth;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Admins;

/// <summary>Removes a colleague's membership and their identity provider user. An Admin cannot remove themselves.</summary>
public static class RemoveAdmin
{
    public static RouteGroupBuilder MapRemoveAdmin(this RouteGroupBuilder admin)
    {
        admin.MapDelete("/admins/{id:guid}", Handle).WithName("RemoveAdmin");
        return admin;
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id, ClaimsPrincipal user, ICurrentTenant currentTenant, CatalogDbContext db,
        IIdentityProvider identityProvider, CancellationToken ct)
    {
        var membership = await db.AdminMemberships
            .SingleOrDefaultAsync(m => m.Id == id && m.TenantId == currentTenant.TenantId, ct);
        if (membership is null)
        {
            return TypedResults.NotFound();
        }

        if (membership.UserId == user.GetUserId())
        {
            return TypedResults.ValidationProblem(
                new ValidationErrors().Add("id", "An Admin cannot remove themselves.").ToDictionary());
        }

        db.AdminMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
        await identityProvider.DeleteUserAsync(membership.UserId, ct);

        return TypedResults.NoContent();
    }
}
