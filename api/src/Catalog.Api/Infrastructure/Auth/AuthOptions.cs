namespace Catalog.Api.Infrastructure.Auth;

/// <summary>
/// Settings for validating identity provider tokens (ADR 0004), bound from the "Auth" section.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string? Issuer { get; set; }
    public string Audience { get; set; } = "authenticated";

    /// <summary>JWKS endpoint for asymmetric keys (hosted Supabase).</summary>
    public string? JwksUrl { get; set; }

    /// <summary>Shared HS256 secret (local Supabase).</summary>
    public string? JwtSecret { get; set; }

    /// <summary>Platform Admins are configured, not stored.</summary>
    public List<Guid> PlatformAdminUserIds { get; set; } = [];
}
