using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Features.Admin.Admins;

/// <summary>An Admin invites a colleague to their own Tenant.</summary>
public static class InviteAdmin
{
    public sealed record Request(string? Email);

    public static RouteGroupBuilder MapInviteAdmin(this RouteGroupBuilder admin)
    {
        admin.MapPost("/admins", Handle).WithName("InviteAdmin");
        return admin;
    }

    private static async Task<Results<Created<AdminInvitation.AdminResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        Request request, ICurrentTenant currentTenant, CatalogDbContext db, IIdentityProvider identityProvider, CancellationToken ct)
    {
        var tenantId = currentTenant.TenantId!.Value;
        return await AdminInvitation.InviteAsync(tenantId, request.Email, db, identityProvider, ct) switch
        {
            AdminInvitation.Outcome.Invited invited => TypedResults.Created($"/api/admin/admins/{invited.Admin.Id}", invited.Admin),
            AdminInvitation.Outcome.Invalid invalid => TypedResults.ValidationProblem(invalid.Errors),
            AdminInvitation.Outcome.AlreadyAdmin conflict => TypedResults.Conflict(conflict.Problem),
            _ => throw new InvalidOperationException("Unexpected invitation outcome."),
        };
    }
}
