using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Features.Public;

/// <summary>
/// The sitemap index (ADR 0013): the Sitemap of every active Tenant. It only reads Tenant, which is not
/// tenant-scoped. Not part of the OpenAPI document.
/// </summary>
public static class GetSitemapIndex
{
    public static IEndpointRouteBuilder MapGetSitemapIndex(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public-sitemap.xml", Handle)
            .WithName("GetSitemapIndex")
            .WithTags("Public")
            .AllowAnonymous()
            .ExcludeFromDescription();
        return app;
    }

    private static async Task<IResult> Handle(
        HttpRequest request, CatalogDbContext db, IOptions<PublicSiteOptions> site, CancellationToken ct)
    {
        if (site.Value.NormalizedBaseUrl is null)
        {
            return TypedResults.NotFound();
        }

        var slugs = await db.Tenants.Where(t => t.IsActive).OrderBy(t => t.Slug).Select(t => t.Slug).ToListAsync(ct);
        var apiBase = $"{request.Scheme}://{request.Host}";
        var xml = GetSitemap.Write(writer =>
        {
            writer.WriteStartElement("sitemapindex", GetSitemap.Namespace);
            foreach (var slug in slugs)
            {
                writer.WriteStartElement("sitemap");
                writer.WriteElementString("loc", $"{apiBase}/api/public/{slug}/sitemap.xml");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        });
        return TypedResults.Content(xml, "application/xml; charset=utf-8");
    }
}
