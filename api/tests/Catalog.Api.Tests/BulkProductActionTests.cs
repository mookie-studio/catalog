using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class BulkProductActionTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Publishing_reports_products_that_cannot_be_published_and_publishes_the_rest()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var one = await CreateProduct(a.Client, "One", "1", "Description");
        var two = await CreateProduct(a.Client, "Two", "2", "Description");
        var bare = await CreateProduct(a.Client, "Bare", "3");

        var result = await Bulk(a.Client, new[] { one.Id, two.Id, bare.Id }, "Publish");

        Assert.Equal([one.Id, two.Id], result.Succeeded);
        var failure = Assert.Single(result.Failed);
        Assert.Equal(bare.Id, failure.ProductId);
        Assert.Contains("description", failure.Errors.Keys);
        var published = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        Assert.Equal(2, published!.TotalCount);
    }

    [Fact]
    public async Task Archive_and_MoveToDraft_change_every_listed_product()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var one = await CreatePublishedProduct(a.Client, "One", "1");
        var two = await CreatePublishedProduct(a.Client, "Two", "2");

        await Bulk(a.Client, new[] { one.Id, two.Id }, "Archive");
        Assert.Equal(2, (await List(a.Client, "?status=Archived")).TotalCount);

        var back = await Bulk(a.Client, new[] { one.Id, two.Id }, "MoveToDraft");
        Assert.Equal(2, back.Succeeded.Count);
        Assert.Equal(2, (await List(a.Client, "?status=Draft")).TotalCount);
    }

    [Fact]
    public async Task A_product_already_in_the_target_state_succeeds_without_changing_UpdatedAt()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreatePublishedProduct(a.Client, "One", "1");
        var before = (await List(a.Client, "")).Items.Single().UpdatedAt;

        var result = await Bulk(a.Client, new[] { product.Id }, "Publish");

        Assert.Equal([product.Id], result.Succeeded);
        Assert.Equal(before, (await List(a.Client, "")).Items.Single().UpdatedAt);
    }

    [Fact]
    public async Task Feature_and_Unfeature_mark_and_clear_the_products()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var one = await CreateProduct(a.Client, "One", "1");
        var two = await CreateProduct(a.Client, "Two", "2");

        await Bulk(a.Client, new[] { one.Id, two.Id }, "Feature");
        Assert.Equal(2, (await List(a.Client, "?featured=true")).TotalCount);

        await Bulk(a.Client, new[] { one.Id }, "Unfeature");
        Assert.Equal([two.Id], (await List(a.Client, "?featured=true")).Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Category_actions_add_and_remove_only_that_Category()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var garden = await CreateCategory(a.Client, "Garden");
        var one = await CreateProduct(a.Client, "One", "1");
        var two = await CreateProduct(a.Client, "Two", "2");
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{one.Id}/categories", new { categoryIds = new[] { tools.Id, garden.Id } })).EnsureSuccessStatusCode();

        var added = await Bulk(a.Client, new[] { one.Id, two.Id }, "AddToCategory", tools.Id);
        Assert.Equal(2, added.Succeeded.Count);
        Assert.Equal(2, (await List(a.Client, $"?categoryId={tools.Id}")).TotalCount);
        Assert.Equal([one.Id], (await List(a.Client, $"?categoryId={garden.Id}")).Items.Select(p => p.Id));

        await Bulk(a.Client, new[] { one.Id, two.Id }, "RemoveFromCategory", tools.Id);
        Assert.Equal(0, (await List(a.Client, $"?categoryId={tools.Id}")).TotalCount);
        Assert.Equal([one.Id], (await List(a.Client, $"?categoryId={garden.Id}")).Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Invalid_requests_return_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "One", "1");
        var foreignCategory = await CreateCategory(b.Client, "Foreign");
        var tooMany = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray();

        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, Array.Empty<Guid>(), "Publish")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, tooMany, "Publish")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, new[] { product.Id, product.Id }, "Publish")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, new[] { product.Id }, "Explode")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, new[] { product.Id }, "AddToCategory")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, new[] { product.Id }, "AddToCategory", Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(a.Client, new[] { product.Id }, "RemoveFromCategory", foreignCategory.Id)).StatusCode);
    }

    [Fact]
    public async Task Isolation_products_of_B_are_reported_as_not_found_and_stay_unchanged()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var mine = await CreateProduct(a.Client, "Mine", "1", "Description");
        var theirs = await CreateProduct(b.Client, "Theirs", "2", "Description");
        var missing = Guid.NewGuid();

        var result = await Bulk(a.Client, new[] { mine.Id, theirs.Id, missing }, "Publish");

        Assert.Equal([mine.Id], result.Succeeded);
        Assert.Equal([theirs.Id, missing], result.Failed.Select(f => f.ProductId));
        Assert.All(result.Failed, f => Assert.Equal("Product not found.", f.Errors["productId"].Single()));
        Assert.Equal("Draft", (await List(b.Client, "")).Items.Single().Status);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, Guid[] ids, string action, Guid? categoryId = null) =>
        client.PostAsJsonAsync("/api/admin/products/bulk", new { productIds = ids, action, categoryId });

    private static async Task<BulkResultBody> Bulk(HttpClient client, Guid[] ids, string action, Guid? categoryId = null)
    {
        var response = await Post(client, ids, action, categoryId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BulkResultBody>())!;
    }

    private static async Task<PagedBody<ProductListItem>> List(HttpClient client, string query) =>
        (await client.GetFromJsonAsync<PagedBody<ProductListItem>>($"/api/admin/products{query}"))!;
}
