using System.Net.Mail;
using System.Text.RegularExpressions;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Profile;

/// <summary>Reads and replaces the current Tenant's Tenant profile (see GLOSSARY.md).</summary>
public static partial class TenantProfile
{
    public sealed record Request(
        string? Description, string? ContactEmail, string? Phone, string? Address, string? WebsiteUrl, string? BrandColor);

    public sealed record Response(
        string Name,
        string Slug,
        string? Description,
        string? ContactEmail,
        string? Phone,
        string? Address,
        string? WebsiteUrl,
        string? BrandColor,
        string? LogoUrl);

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex BrandColorPattern();

    public static RouteGroupBuilder MapTenantProfile(this RouteGroupBuilder admin)
    {
        admin.MapGet("/tenant/profile", Get).WithName("GetTenantProfile");
        admin.MapPut("/tenant/profile", Update).WithName("UpdateTenantProfile");
        return admin;
    }

    private static async Task<Results<Ok<Response>, NotFound>> Get(
        ICurrentTenant currentTenant, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == currentTenant.TenantId, ct);
        return tenant is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(tenant, storage));
    }

    private static async Task<Results<Ok<Response>, ValidationProblem, NotFound>> Update(
        Request request, ICurrentTenant currentTenant, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var description = Clean(request.Description);
        var contactEmail = Clean(request.ContactEmail);
        var phone = Clean(request.Phone);
        var address = Clean(request.Address);
        var websiteUrl = Clean(request.WebsiteUrl);
        var brandColor = Clean(request.BrandColor);

        var errors = new ValidationErrors();
        MaxLength(description, Tenant.DescriptionMaxLength, "description", "description", errors);
        MaxLength(phone, Tenant.PhoneMaxLength, "phone", "phone", errors);
        MaxLength(address, Tenant.AddressMaxLength, "address", "address", errors);
        if (contactEmail is not null && !IsValidEmail(contactEmail))
        {
            errors.Add("contactEmail", "The contact email is not a valid email address.");
        }

        if (websiteUrl is not null && !IsValidWebsiteUrl(websiteUrl))
        {
            errors.Add("websiteUrl", "The website must be an absolute http or https URL of at most 300 characters.");
        }

        if (brandColor is not null && !BrandColorPattern().IsMatch(brandColor))
        {
            errors.Add("brandColor", "The brand color must be a hex color like #1A2B3C.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == currentTenant.TenantId, ct);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        tenant.Description = description;
        tenant.ContactEmail = contactEmail;
        tenant.Phone = phone;
        tenant.Address = address;
        tenant.WebsiteUrl = websiteUrl;
        tenant.BrandColor = brandColor?.ToUpperInvariant();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ToResponse(tenant, storage));
    }

    private static Response ToResponse(Tenant t, IImageStorage storage) =>
        new(
            t.Name, t.Slug, t.Description, t.ContactEmail, t.Phone, t.Address, t.WebsiteUrl, t.BrandColor,
            LogoUrl(t.LogoKey, storage));

    /// <summary>The public URL of a Logo, or null when the tenant has none.</summary>
    public static string? LogoUrl(string? logoKey, IImageStorage storage) =>
        logoKey is null ? null : storage.GetPublicUrl(logoKey);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void MaxLength(string? value, int max, string field, string label, ValidationErrors errors)
    {
        if (value is not null && value.Length > max)
        {
            errors.Add(field, $"The {label} must be at most {max} characters.");
        }
    }

    private static bool IsValidEmail(string email) =>
        email.Length <= Tenant.ContactEmailMaxLength
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && address.Host.Contains('.', StringComparison.Ordinal);

    private static bool IsValidWebsiteUrl(string url) =>
        url.Length <= Tenant.WebsiteUrlMaxLength
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && !string.IsNullOrEmpty(uri.Host);
}
