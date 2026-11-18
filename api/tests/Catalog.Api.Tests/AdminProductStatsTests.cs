using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminProductStatsTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static readonly object Inquiry = new { name = "Ana", email = "ana@example.com", message = "Hello there" };

    [Fact]
    public async Task Products_are_ranked_by_views_then_Inquiries_and_unseen_ones_are_absent()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        (await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true })).EnsureSuccessStatusCode();
        var hammer = await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        var saw = await CreatePublishedProduct(a.Client, "Saw", "S-1");
        var drill = await CreatePublishedProduct(a.Client, "Drill", "D-1");
        await CreatePublishedProduct(a.Client, "Unseen", "U-1");
        await View("hammer", 3);
        await View("saw", 3);
        await View("drill", 1);
        (await Client.PostAsJsonAsync("/api/public/tenant-a/products/saw/inquiries", Inquiry)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync("/api/public/tenant-a/products/drill/inquiries", Inquiry)).EnsureSuccessStatusCode();

        var stats = await Stats(a.Client, "");

        Assert.Equal([saw.Id, hammer.Id, drill.Id], stats.Items.Select(s => s.ProductId));
        Assert.Equal([(3, 1), (3, 0), (1, 1)], stats.Items.Select(s => (s.Views, s.Inquiries)));
        Assert.Equal(3, stats.TotalCount);
        Assert.Equal([hammer.Id], (await Stats(a.Client, "?pageSize=1&page=2")).Items.Select(s => s.ProductId));
    }

    [Fact]
    public async Task A_range_ending_yesterday_leaves_out_todays_views()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await View("hammer", 2);
        var today = DateTime.UtcNow.Date;

        var untilYesterday = await Stats(a.Client, $"?to={today.AddDays(-1):yyyy-MM-dd}");
        var includingToday = await Stats(a.Client, $"?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        Assert.Empty(untilYesterday.Items);
        Assert.Equal(2, includingToday.Items.Single().Views);
    }

    [Fact]
    public async Task Invalid_ranges_return_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        foreach (var query in new[] { "?from=yesterday", "?to=2026-13-40", "?from=2026-02-01&to=2026-01-01", "?from=2024-01-01&to=2026-01-01" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await a.Client.GetAsync($"/api/admin/stats/products{query}")).StatusCode);
        }
    }

    [Fact]
    public async Task Isolation_views_and_Inquiries_of_B_never_appear_for_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        (await b.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true })).EnsureSuccessStatusCode();
        await CreatePublishedProduct(b.Client, "Hammer", "H-1");
        (await Client.PostAsync("/api/public/tenant-b/products/hammer/views", null)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync("/api/public/tenant-b/products/hammer/inquiries", Inquiry)).EnsureSuccessStatusCode();

        Assert.Empty((await Stats(a.Client, "")).Items);
        Assert.Single((await Stats(b.Client, "")).Items);
    }

    private async Task View(string product, int times)
    {
        for (var i = 0; i < times; i++)
        {
            (await Client.PostAsync($"/api/public/tenant-a/products/{product}/views", null)).EnsureSuccessStatusCode();
        }
    }

    private static async Task<PagedBody<ProductStatBody>> Stats(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<PagedBody<ProductStatBody>>($"/api/admin/stats/products{query}"))!;
}
