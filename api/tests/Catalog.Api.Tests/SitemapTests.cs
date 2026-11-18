using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;

namespace Catalog.Api.Tests;

public sealed class SitemapTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private HttpClient WithSite(string? baseUrl = "https://shop.example.com/") =>
        Factory.WithWebHostBuilder(builder => builder.UseSetting("PublicSite:BaseUrl", baseUrl)).CreateClient();

    [Fact]
    public async Task The_Sitemap_lists_the_home_and_exactly_the_Published_products()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Saw & Co <big>", "S-1");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await CreateProduct(a.Client, "Draft", "D-1");
        var archived = await CreatePublishedProduct(a.Client, "Archived", "A-1");
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{archived.Id}/status", new { status = "Archived" })).EnsureSuccessStatusCode();
        var client = WithSite();

        var response = await client.GetAsync("/api/public/tenant-a/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/xml", response.Content.Headers.ContentType!.ToString());
        var urls = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!.Elements(Ns + "url").ToList();
        Assert.Equal(
            ["https://shop.example.com/tenant-a", "https://shop.example.com/tenant-a/p/hammer", "https://shop.example.com/tenant-a/p/saw-co-big"],
            urls.Select(u => u.Element(Ns + "loc")!.Value));
        Assert.Null(urls[0].Element(Ns + "lastmod"));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", urls[1].Element(Ns + "lastmod")!.Value);
    }

    [Fact]
    public async Task An_Old_Tenant_slug_yields_URLs_with_the_current_slug()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        (await PlatformAdminClient().PutAsJsonAsync($"/api/platform/tenants/{a.Tenant.Id}/slug", new { slug = "tenant-z" })).EnsureSuccessStatusCode();
        var client = WithSite();

        var doc = XDocument.Parse(await client.GetStringAsync("/api/public/tenant-a/sitemap.xml"));

        Assert.All(doc.Root!.Descendants(Ns + "loc"), loc => Assert.StartsWith("https://shop.example.com/tenant-z", loc.Value));
    }

    [Fact]
    public async Task Without_PublicSite_BaseUrl_or_for_a_Deactivated_tenant_the_Sitemap_is_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/sitemap.xml")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await WithSite("not a url").GetAsync("/api/public/tenant-a/sitemap.xml")).StatusCode);

        (await PlatformAdminClient().PostAsync($"/api/platform/tenants/{a.Tenant.Id}/deactivate", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await WithSite().GetAsync("/api/public/tenant-a/sitemap.xml")).StatusCode);
    }

    [Fact]
    public async Task The_index_lists_exactly_the_active_tenants_and_each_entry_works()
    {
        await CreateTenantWithAdmin("tenant-b");
        await CreateTenantWithAdmin("tenant-a");
        var gone = await CreateTenantWithAdmin("tenant-c");
        (await PlatformAdminClient().PostAsync($"/api/platform/tenants/{gone.Tenant.Id}/deactivate", null)).EnsureSuccessStatusCode();
        var client = WithSite();

        var response = await client.GetAsync("/api/public-sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var locs = XDocument.Parse(await response.Content.ReadAsStringAsync()).Root!
            .Elements(Ns + "sitemap").Select(s => new Uri(s.Element(Ns + "loc")!.Value)).ToList();
        Assert.Equal(["/api/public/tenant-a/sitemap.xml", "/api/public/tenant-b/sitemap.xml"], locs.Select(l => l.AbsolutePath));
        foreach (var loc in locs)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(loc.AbsolutePath)).StatusCode);
        }
    }

    [Fact]
    public async Task The_index_is_404_without_PublicSite_BaseUrl()
    {
        await CreateTenantWithAdmin("tenant-a");

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public-sitemap.xml")).StatusCode);
    }
}
