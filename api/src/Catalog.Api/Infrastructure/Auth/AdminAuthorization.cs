using Catalog.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Infrastructure.Auth;

/// <summary>Requires an AdminMembership whose Tenant is active; on success it sets the current Tenant (ADR 0007).</summary>
public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminHandler(CatalogDbContext db, ICurrentTenant currentTenant) : AuthorizationHandler<AdminRequirement>
{
    public const string TenantDeactivatedReason = "Tenant deactivated";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.GetUserId() is not { } userId)
        {
            return;
        }

        var membership = await db.AdminMemberships
            .Where(m => m.UserId == userId)
            .Join(db.Tenants, m => m.TenantId, t => t.Id, (m, t) => new { t.Id, t.IsActive })
            .SingleOrDefaultAsync();

        if (membership is null)
        {
            return;
        }

        if (!membership.IsActive)
        {
            context.Fail(new AuthorizationFailureReason(this, TenantDeactivatedReason));
            return;
        }

        currentTenant.Set(membership.Id);
        context.Succeed(requirement);
    }
}

/// <summary>Turns a "Tenant deactivated" authorization failure into a 403 ProblemDetails with that title.</summary>
public sealed class CatalogAuthorizationResultHandler(IProblemDetailsService problemDetails) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var deactivated = authorizeResult.Forbidden && authorizeResult.AuthorizationFailure?.FailureReasons
            .Any(r => r.Message == AdminHandler.TenantDeactivatedReason) == true;

        if (!deactivated)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = AdminHandler.TenantDeactivatedReason,
                Detail = "The Tenant of this Admin has been deactivated by a Platform Admin.",
            },
        });
    }
}
