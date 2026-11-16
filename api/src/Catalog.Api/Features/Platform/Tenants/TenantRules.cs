using System.Text.RegularExpressions;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;

namespace Catalog.Api.Features.Platform.Tenants;

/// <summary>Rules and response shape shared by the Tenant slices.</summary>
public static partial class TenantRules
{
    public const int SlugMinLength = 3;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    public static void ValidateSlug(string? slug, ValidationErrors errors, string field = "slug")
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            errors.Add(field, "The Tenant slug is required.");
        }
        else if (slug.Length is < SlugMinLength or > Tenant.SlugMaxLength)
        {
            errors.Add(field, $"The Tenant slug must be between {SlugMinLength} and {Tenant.SlugMaxLength} characters.");
        }
        else if (!SlugPattern().IsMatch(slug))
        {
            errors.Add(field, "The Tenant slug may only contain lowercase letters, digits and single hyphens between them.");
        }
    }

    public static void ValidateName(string? name, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "The name is required.");
        }
        else if (name.Trim().Length > Tenant.NameMaxLength)
        {
            errors.Add("name", $"The name must be at most {Tenant.NameMaxLength} characters.");
        }
    }
}

public sealed record TenantResponse(Guid Id, string Name, string Slug, bool IsActive, DateTimeOffset CreatedAt)
{
    public static TenantResponse From(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt);
}
