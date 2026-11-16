using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Infrastructure.Auth;

public sealed class PlatformAdminRequirement : IAuthorizationRequirement;

public sealed class PlatformAdminHandler(IOptionsMonitor<AuthOptions> options)
    : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PlatformAdminRequirement requirement)
    {
        if (IsPlatformAdmin(options.CurrentValue, context.User.GetUserId()))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    public static bool IsPlatformAdmin(AuthOptions options, Guid? userId) =>
        userId is { } id && options.PlatformAdminUserIds.Contains(id);
}
