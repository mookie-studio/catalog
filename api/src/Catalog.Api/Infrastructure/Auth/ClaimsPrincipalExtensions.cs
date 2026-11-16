using System.Security.Claims;

namespace Catalog.Api.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The identity provider's user id (the "sub" claim), or null when missing or not a Guid.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimNames.UserId), out var id) ? id : null;

    public static string? GetEmail(this ClaimsPrincipal user) => user.FindFirstValue(ClaimNames.Email);
}
