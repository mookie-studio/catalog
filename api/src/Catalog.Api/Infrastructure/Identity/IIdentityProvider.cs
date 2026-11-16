namespace Catalog.Api.Infrastructure.Identity;

/// <summary>
/// The identity provider that authenticates Admins (ADR 0004). Catalog only invites and deletes users;
/// tenant and role live in Catalog's own database.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>Sends an invitation email and returns the new (or existing) user id.</summary>
    Task<Guid> InviteUserAsync(string email, CancellationToken ct);

    Task DeleteUserAsync(Guid userId, CancellationToken ct);
}
