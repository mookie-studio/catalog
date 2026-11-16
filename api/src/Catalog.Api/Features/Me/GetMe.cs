using System.Security.Claims;
using Catalog.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Features.Me;

public static class GetMe
{
    public sealed record Response(Guid UserId, string? Email, bool IsPlatformAdmin);

    public static IEndpointRouteBuilder MapGetMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", Handle)
            .RequireAuthorization()
            .WithName("GetMe")
            .WithTags("Me");

        return app;
    }

    private static Results<Ok<Response>, UnauthorizedHttpResult> Handle(
        ClaimsPrincipal user, IOptionsMonitor<AuthOptions> authOptions)
    {
        if (user.GetUserId() is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var isPlatformAdmin = PlatformAdminHandler.IsPlatformAdmin(authOptions.CurrentValue, userId);
        return TypedResults.Ok(new Response(userId, user.GetEmail(), isPlatformAdmin));
    }
}
