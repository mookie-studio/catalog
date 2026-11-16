using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.Api.Tests;

/// <summary>Mints HS256 tokens shaped like the identity provider's (sub, email, aud).</summary>
public static class TestTokens
{
    public const string Secret = "catalog-tests-secret-that-is-long-enough-for-hs256";
    public const string Issuer = "https://identity.catalog.test/auth/v1";
    public const string Audience = "authenticated";

    public static readonly Guid PlatformAdminUserId = Guid.Parse("0195c0de-0000-7000-8000-000000000001");

    public static string Create(
        Guid userId,
        string email,
        DateTime? expires = null,
        string secret = Secret)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var expiresAt = expires ?? now.AddHours(1);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", userId.ToString()), new Claim("email", email)],
            notBefore: expiresAt < now ? expiresAt.AddHours(-1) : now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
