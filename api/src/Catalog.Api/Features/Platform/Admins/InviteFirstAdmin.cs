using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Platform.Admins;

/// <summary>A Platform Admin invites an Admin (typically the first one) of a Tenant.</summary>
public static class InviteFirstAdmin
{
    public sealed record Request(string? Email);

    public static RouteGroupBuilder MapInviteFirstAdmin(this RouteGroupBuilder platform)
    {
        platform.MapPost("/tenants/{id:guid}/admins", Handle).WithName("PlatformInviteAdmin");
        return platform;
    }

    private static async Task<Results<Created<AdminInvitation.AdminResponse>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, Request request, CatalogDbContext db, IIdentityProvider identityProvider, CancellationToken ct)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == id, ct))
        {
            return TypedResults.NotFound();
        }

        return await AdminInvitation.InviteAsync(id, request.Email, db, identityProvider, ct) switch
        {
            AdminInvitation.Outcome.Invited invited =>
                TypedResults.Created($"/api/platform/tenants/{id}/admins/{invited.Admin.Id}", invited.Admin),
            AdminInvitation.Outcome.Invalid invalid => TypedResults.ValidationProblem(invalid.Errors),
            AdminInvitation.Outcome.AlreadyAdmin conflict => TypedResults.Conflict(conflict.Problem),
            _ => throw new InvalidOperationException("Unexpected invitation outcome."),
        };
    }
}
