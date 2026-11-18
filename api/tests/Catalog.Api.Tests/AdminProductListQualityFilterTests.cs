using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminProductListQualityFilterTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Filters_by_having_Images_and_Categories_and_combine_with_status()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var category = await CreateCategory(a.Client, "Tools");
        var full = await CreatePublishedProduct(a.Client, "Full", "F-1");
        var bare = await CreatePublishedProduct(a.Client, "Bare", "B-1");
        var draft = await CreateProduct(a.Client, "Draft", "D-1");
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{full.Id}/categories", new { categoryIds = new[] { category.Id } })).EnsureSuccessStatusCode();
        (await a.Client.PostAsync($"/api/admin/products/{full.Id}/images", TestImages.Form(TestImages.Png(), "a.png"))).EnsureSuccessStatusCode();

        Assert.Equal([full.Id], Ids(await List(a.Client, "?hasImages=true")));
        Assert.Equal([full.Id], Ids(await List(a.Client, "?hasCategories=true")));
        var withoutImages = await List(a.Client, "?hasImages=false");
        Assert.Equal(2, withoutImages.TotalCount);
        Assert.Equal([bare.Id], Ids(await List(a.Client, "?status=Published&hasImages=false")));
        Assert.Equal([bare.Id], Ids(await List(a.Client, "?status=Published&hasCategories=false")));
        Assert.Equal(3, (await List(a.Client, "")).TotalCount);
        Assert.Contains(draft.Id, Ids(await List(a.Client, "?hasCategories=false")));
    }

    [Fact]
    public async Task Isolation_products_of_B_never_appear_for_an_Admin_of_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateProduct(b.Client, "Hammer", "SKU-1");

        Assert.Equal(0, (await List(a.Client, "?hasImages=false")).TotalCount);
        Assert.Equal(0, (await List(a.Client, "?hasCategories=false")).TotalCount);
    }

    private static List<Guid> Ids(PagedBody<ProductListItem> page) => page.Items.Select(p => p.Id).ToList();

    private static async Task<PagedBody<ProductListItem>> List(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<PagedBody<ProductListItem>>($"/api/admin/products{query}"))!;
}
