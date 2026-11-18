using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class RelatedProductsTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Products_sharing_more_Categories_come_first_then_Featured_then_name()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var garden = await CreateCategory(a.Client, "Garden");
        var other = await CreateCategory(a.Client, "Other");
        var main = await Published(a.Client, "Main", "M", tools, garden);
        await Published(a.Client, "Both", "B", tools, garden);
        await Published(a.Client, "One featured", "F", tools);
        await Published(a.Client, "One alpha", "A", garden);
        await Published(a.Client, "Unrelated", "U", other);
        await Published(a.Client, "No categories", "N");
        var featured = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        Assert.Equal(6, featured!.TotalCount);
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{await IdOf(a.Client, "F")}/featured", new { featured = true })).EnsureSuccessStatusCode();

        var related = await Related("tenant-a", "main");

        Assert.Equal(["both", "one-featured", "one-alpha"], related.Items.Select(i => i.Slug));
        Assert.Equal(3, related.TotalCount);
        Assert.DoesNotContain(related.Items, i => i.Slug == main.Slug);
    }

    [Fact]
    public async Task Drafts_and_Archived_products_are_never_returned_and_paging_applies()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        await Published(a.Client, "Main", "M", tools);
        var draft = await CreateProduct(a.Client, "Draft", "D");
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{draft.Id}/categories", new { categoryIds = new[] { tools.Id } })).EnsureSuccessStatusCode();
        var archived = await Published(a.Client, "Archived", "A", tools);
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{archived.Id}/status", new { status = "Archived" })).EnsureSuccessStatusCode();
        await Published(a.Client, "Alpha", "AL", tools);
        await Published(a.Client, "Beta", "BE", tools);

        var first = await Related("tenant-a", "main", "?pageSize=1");
        var second = await Related("tenant-a", "main", "?pageSize=1&page=2");

        Assert.Equal(2, first.TotalCount);
        Assert.Equal(["alpha"], first.Items.Select(i => i.Slug));
        Assert.Equal(["beta"], second.Items.Select(i => i.Slug));
    }

    [Fact]
    public async Task A_product_without_Categories_has_none_and_unpublished_products_are_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        await Published(a.Client, "Lonely", "L");
        await Published(a.Client, "Other", "O", tools);
        await CreateProduct(a.Client, "Draft", "D");

        Assert.Empty((await Related("tenant-a", "lonely")).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/draft/related")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/nothing/related")).StatusCode);
    }

    [Fact]
    public async Task An_Old_Product_slug_resolves()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var main = await Published(a.Client, "Main", "M", tools);
        await Published(a.Client, "Other", "O", tools);
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{main.Id}", new { name = "Main", sku = "M", description = "Description", slug = "primary" }))
            .EnsureSuccessStatusCode();

        Assert.Equal(["other"], (await Related("tenant-a", "main")).Items.Select(i => i.Slug));
    }

    [Fact]
    public async Task Isolation_products_of_B_are_never_related_to_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var toolsA = await CreateCategory(a.Client, "Tools");
        var toolsB = await CreateCategory(b.Client, "Tools");
        await Published(a.Client, "Main", "M", toolsA);
        await Published(b.Client, "Theirs", "T", toolsB);

        Assert.Empty((await Related("tenant-a", "main")).Items);
    }

    private async Task<PublicProductPage> Related(string tenant, string product, string query = "") =>
        (await Client.GetFromJsonAsync<PublicProductPage>($"/api/public/{tenant}/products/{product}/related{query}"))!;

    private static async Task<ProductBody> Published(HttpClient client, string name, string sku, params CategoryBody[] categories)
    {
        var product = await CreatePublishedProduct(client, name, sku);
        if (categories.Length > 0)
        {
            (await client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = categories.Select(c => c.Id) })).EnsureSuccessStatusCode();
        }

        return product;
    }

    private static async Task<Guid> IdOf(HttpClient client, string sku) =>
        (await client.GetFromJsonAsync<PagedBody<ProductListItem>>($"/api/admin/products?q={sku}"))!.Items.First(p => p.Sku == sku).Id;
}
