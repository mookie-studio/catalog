using System.Text;
using System.Xml;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Features.Public;

/// <summary>
/// The Sitemap of a Tenant (ADR 0013): its catalog home and every Published product, with their SPA addresses.
/// Not part of the OpenAPI document; search engines read it.
/// </summary>
public static class GetSitemap
{
    public const int MaxProducts = 49_999;
    public const string Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static RouteGroupBuilder MapGetSitemap(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/sitemap.xml", Handle).WithName("GetSitemap").ExcludeFromDescription();
        return publicGroup;
    }

    private static async Task<IResult> Handle(
        ICurrentTenant currentTenant, CatalogDbContext db, IOptions<PublicSiteOptions> site, CancellationToken ct)
    {
        var baseUrl = site.Value.NormalizedBaseUrl;
        if (baseUrl is null)
        {
            return TypedResults.NotFound();
        }

        // The current slug, even when the request used an Old Tenant slug.
        var tenantSlug = await db.Tenants.Where(t => t.Id == currentTenant.TenantId).Select(t => t.Slug).SingleAsync(ct);
        var products = await db.Products
            .Where(p => p.Status == ProductStatus.Published)
            .OrderBy(p => p.Slug)
            .Take(MaxProducts)
            .Select(p => new { p.Slug, p.UpdatedAt })
            .ToListAsync(ct);

        var xml = Write(writer =>
        {
            writer.WriteStartElement("urlset", Namespace);
            WriteUrl(writer, $"{baseUrl}/{tenantSlug}", null);
            foreach (var product in products)
            {
                WriteUrl(writer, $"{baseUrl}/{tenantSlug}/p/{product.Slug}", product.UpdatedAt);
            }

            writer.WriteEndElement();
        });
        return TypedResults.Content(xml, "application/xml; charset=utf-8");
    }

    internal static string Write(Action<XmlWriter> body)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false }))
        {
            writer.WriteStartDocument();
            body(writer);
            writer.WriteEndDocument();
        }

        return builder.ToString();
    }

    private static void WriteUrl(XmlWriter writer, string location, DateTimeOffset? lastModified)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", location);
        if (lastModified is { } modified)
        {
            writer.WriteElementString("lastmod", modified.UtcDateTime.ToString("yyyy-MM-dd"));
        }

        writer.WriteEndElement();
    }
}
