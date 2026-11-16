namespace Catalog.Api.Data;

/// <summary>
/// Maps an identity provider user to the one Tenant they administer (ADR 0004).
/// It is how the current Tenant is resolved, so it is deliberately not ITenantScoped:
/// admin slices filter it by the current Tenant explicitly.
/// </summary>
public sealed class AdminMembership
{
    public const int EmailMaxLength = 254;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
}
