using System.Security.Claims;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Features.Me;

public static class GetMe
{
    public sealed record Response(Guid UserId, string? Email, bool IsPlatformAdmin, Guid? TenantId);

    public static IEndpointRouteBuilder MapGetMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", Handle)
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Me");

        return app;
    }

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(
        ClaimsPrincipal user, IOptionsMonitor<AuthOptions> authOptions, CatalogDbContext db, CancellationToken ct)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var isPlatformAdmin = PlatformAdminHandler.IsPlatformAdmin(authOptions.CurrentValue, userId);
        var tenantId = await db.AdminMemberships
            .Where(m => m.UserId == userId)
            .Select(m => (Guid?)m.TenantId)
            .SingleOrDefaultAsync(ct);
        return TypedResults.Ok(new Response(userId, user.GetEmail(), isPlatformAdmin, tenantId));
    }
}
