using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class ProductViewTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Viewing_a_Published_product_returns_202_and_other_products_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await CreateProduct(a.Client, "Draft", "D-1");
        var archived = await CreatePublishedProduct(a.Client, "Archived", "A-1");
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{archived.Id}/status", new { status = "Archived" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Accepted, (await View("tenant-a", "hammer")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await View("tenant-a", "draft")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await View("tenant-a", "archived")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await View("tenant-a", "nothing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await View("unknown", "hammer")).StatusCode);
    }

    [Fact]
    public async Task Views_are_counted_per_product_including_concurrent_ones()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var hammer = await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await CreatePublishedProduct(a.Client, "Saw", "S-1");

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => View("tenant-a", "hammer")));
        await View("tenant-a", "saw");

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Accepted, r.StatusCode));
        var stats = await Stats(a.Client, "");
        Assert.Equal(20, stats.Items.Single(s => s.ProductId == hammer.Id).Views);
        Assert.Equal(1, stats.Items.Single(s => s.Slug == "saw").Views);
    }

    [Fact]
    public async Task An_Old_Product_slug_counts_for_the_current_product()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}", new { name = "Hammer", sku = "H-1", description = "Description", slug = "mallet" }))
            .EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Accepted, (await View("tenant-a", "hammer")).StatusCode);

        Assert.Equal(1, (await Stats(a.Client, "")).Items.Single(s => s.ProductId == product.Id).Views);
    }

    [Fact]
    public async Task A_Deactivated_tenant_returns_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        (await PlatformAdminClient().PostAsync($"/api/platform/tenants/{a.Tenant.Id}/deactivate", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NotFound, (await View("tenant-a", "hammer")).StatusCode);
    }

    [Fact]
    public async Task The_third_view_from_one_IP_gets_429_while_other_IPs_and_endpoints_continue()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await using var limited = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimits:ProductViews:PermitLimit", "2"));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await Send(client, "203.0.113.1")).StatusCode);
        }

        Assert.Equal([HttpStatusCode.Accepted, HttpStatusCode.Accepted, HttpStatusCode.TooManyRequests], statuses);
        var rejected = await Send(client, "203.0.113.1");
        Assert.True(rejected.Headers.Contains("Retry-After"));
        Assert.Equal("Too many requests", (await rejected.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
        Assert.Equal(HttpStatusCode.Accepted, (await Send(client, "198.51.100.7")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/tenant-a")).StatusCode);
    }

    private Task<HttpResponseMessage> View(string tenant, string product) =>
        Client.PostAsync($"/api/public/{tenant}/products/{product}/views", null);

    private static Task<HttpResponseMessage> Send(HttpClient client, string ip)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/tenant-a/products/hammer/views");
        request.Headers.Add("X-Forwarded-For", ip);
        return client.SendAsync(request);
    }

    private static async Task<PagedBody<ProductStatBody>> Stats(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<PagedBody<ProductStatBody>>($"/api/admin/stats/products{query}"))!;
}
