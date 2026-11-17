namespace Catalog.Api.Data;

/// <summary>
/// An isolated business inside the platform (see GLOSSARY.md).
/// </summary>
public sealed class Tenant
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 50;
    public const int DescriptionMaxLength = 2000;
    public const int ContactEmailMaxLength = 254;
    public const int PhoneMaxLength = 40;
    public const int AddressMaxLength = 300;
    public const int WebsiteUrlMaxLength = 300;
    public const int BrandColorLength = 7;
    public const int LogoKeyMaxLength = 200;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();

    // Tenant profile (see GLOSSARY.md): what the tenant shows about itself in its public catalog.
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>Brand color as "#RRGGBB", upper-case.</summary>
    public string? BrandColor { get; set; }

    /// <summary>Image storage key of the Logo, if any.</summary>
    public string? LogoKey { get; set; }
}
