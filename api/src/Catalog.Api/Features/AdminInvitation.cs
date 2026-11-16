using System.Net.Mail;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features;

/// <summary>
/// Inviting an Admin to a Tenant, shared by the platform invite (first Admin) and the admin invite (colleagues).
/// An Admin belongs to exactly one Tenant.
/// </summary>
public static class AdminInvitation
{
    public sealed record AdminResponse(Guid Id, Guid UserId, string Email);

    public abstract record Outcome
    {
        public sealed record Invited(AdminResponse Admin) : Outcome;
        public sealed record Invalid(IDictionary<string, string[]> Errors) : Outcome;
        public sealed record AlreadyAdmin(ProblemDetails Problem) : Outcome;
    }

    public static string? Normalize(string? email) => email?.Trim().ToLowerInvariant();

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > AdminMembership.EmailMaxLength)
        {
            return false;
        }

        return MailAddress.TryCreate(email, out var address) && address.Address == email && email.Contains('.', StringComparison.Ordinal);
    }

    public static AdminResponse ToResponse(AdminMembership membership) =>
        new(membership.Id, membership.UserId, membership.Email);

    /// <summary>
    /// Invites <paramref name="rawEmail"/> as an Admin of <paramref name="tenantId"/>.
    /// Uniqueness is checked across all Tenants.
    /// </summary>
    public static async Task<Outcome> InviteAsync(
        Guid tenantId, string? rawEmail, CatalogDbContext db, IIdentityProvider identityProvider, CancellationToken ct)
    {
        var email = Normalize(rawEmail);
        if (!IsValidEmail(email))
        {
            return new Outcome.Invalid(new ValidationErrors().Add("email", "A valid email is required.").ToDictionary());
        }

        if (await db.AdminMemberships.AnyAsync(m => m.Email == email, ct))
        {
            return AlreadyAdmin();
        }

        var userId = await identityProvider.InviteUserAsync(email!, ct);
        if (await db.AdminMemberships.AnyAsync(m => m.UserId == userId, ct))
        {
            return AlreadyAdmin();
        }

        var membership = new AdminMembership { TenantId = tenantId, UserId = userId, Email = email! };
        db.AdminMemberships.Add(membership);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return AlreadyAdmin();
        }

        return new Outcome.Invited(ToResponse(membership));
    }

    private static Outcome.AlreadyAdmin AlreadyAdmin() =>
        new(ProblemDetailsBody.Conflict("This email is already an Admin of a Tenant."));
}
