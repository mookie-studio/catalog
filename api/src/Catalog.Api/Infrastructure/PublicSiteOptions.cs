namespace Catalog.Api.Infrastructure;

/// <summary>Where the public SPA lives; Sitemap addresses point there (ADR 0013).</summary>
public sealed class PublicSiteOptions
{
    public const string SectionName = "PublicSite";

    /// <summary>Absolute http/https URL of the SPA. Empty disables the Sitemap endpoints.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>The configured base URL without a trailing slash, or null when it is missing or not absolute http/https.</summary>
    public string? NormalizedBaseUrl =>
        Uri.TryCreate(BaseUrl?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/')
            : null;
}
