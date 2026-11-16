using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminProductListTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Search_matches_part_of_the_name_or_SKU_case_insensitively()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreateProduct(a.Client, "Hammer Drill", "HD-500");
        var saw = await CreateProduct(a.Client, "Circular Saw", "CS-100");

        var byName = await List(a.Client, "?q=dRiLl");
        var bySku = await List(a.Client, "?q=cs-1");
        var special = await List(a.Client, "?q=%25");

        Assert.Equal([drill.Id], byName.Items.Select(p => p.Id));
        Assert.Equal([saw.Id], bySku.Items.Select(p => p.Id));
        Assert.Empty(special.Items);
    }

    [Fact]
    public async Task Status_filter_returns_only_products_in_that_status()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var published = await CreateProduct(a.Client, "Published", "P-1", "Ready");
        await CreateProduct(a.Client, "Draft", "D-1");
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{published.Id}/status", new { status = "Published" })).EnsureSuccessStatusCode();

        var list = await List(a.Client, "?status=Published");
        var all = await List(a.Client, "");

        Assert.Equal([published.Id], list.Items.Select(p => p.Id));
        Assert.Equal(2, all.TotalCount);
    }

    [Fact]
    public async Task The_list_is_paged_by_most_recently_updated_and_capped_at_50()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var created = new List<ProductBody>();
        for (var i = 1; i <= 55; i++)
        {
            created.Add(await CreateProduct(a.Client, $"Product {i}", $"SKU-{i}"));
        }

        var first = await List(a.Client, "?pageSize=5");
        var second = await List(a.Client, "?page=2&pageSize=5");
        var capped = await List(a.Client, "?pageSize=100");

        Assert.Equal(55, first.TotalCount);
        Assert.Equal(created.AsEnumerable().Reverse().Take(5).Select(p => p.Id), first.Items.Select(p => p.Id));
        Assert.Equal(created.AsEnumerable().Reverse().Skip(5).Take(5).Select(p => p.Id), second.Items.Select(p => p.Id));
        Assert.Equal(50, capped.PageSize);
        Assert.Equal(50, capped.Items.Count);
    }

    [Fact]
    public async Task Isolation_products_of_B_never_appear_for_an_Admin_of_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateProduct(b.Client, "Hammer", "SKU-1");

        var list = await List(a.Client, "?q=hammer");

        Assert.Equal(0, list.TotalCount);
        Assert.Empty(list.Items);
    }

    private static async Task<PagedBody<ProductListItem>> List(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<PagedBody<ProductListItem>>($"/api/admin/products{query}"))!;
}
