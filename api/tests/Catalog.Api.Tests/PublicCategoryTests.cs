using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PublicCategoryTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Categories_without_Published_products_in_their_subtree_are_hidden()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var drills = await CreateCategory(a.Client, "Drills", tools.Id);
        var garden = await CreateCategory(a.Client, "Garden");
        var mowers = await CreateCategory(a.Client, "Mowers", garden.Id);
        await CreateCategory(a.Client, "Empty");
        var drill = await CreatePublishedProduct(a.Client, "Drill", "D-1");
        var mower = await CreateProduct(a.Client, "Mower", "M-1", "Draft only");
        await Assign(a.Client, drill.Id, drills.Id);
        await Assign(a.Client, mower.Id, mowers.Id);

        var tree = await Client.GetFromJsonAsync<List<CategoryBody>>("/api/public/tenant-a/categories");

        var root = Assert.Single(tree!);
        Assert.Equal("Tools", root.Name);
        Assert.Equal("Drills", Assert.Single(root.Children).Name);
    }

    [Fact]
    public async Task Filtering_by_a_parent_Category_includes_products_of_its_subcategories()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var drills = await CreateCategory(a.Client, "Drills", tools.Id);
        var garden = await CreateCategory(a.Client, "Garden");
        var drill = await CreatePublishedProduct(a.Client, "Drill", "D-1");
        var hammer = await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        var rake = await CreatePublishedProduct(a.Client, "Rake", "R-1");
        await Assign(a.Client, drill.Id, drills.Id);
        await Assign(a.Client, hammer.Id, tools.Id);
        await Assign(a.Client, rake.Id, garden.Id);

        Assert.Equal(["Drill", "Hammer"], await Names($"categoryId={tools.Id}"));
        Assert.Equal(["Drill"], await Names($"categoryId={drills.Id}"));
        Assert.Equal(["Drill"], await Names($"categoryId={tools.Id}&q=drill"));
        Assert.Empty(await Names($"categoryId={Guid.CreateVersion7()}"));
    }

    [Fact]
    public async Task A_Tenant_without_Categories_has_an_empty_tree_and_lists_products()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Drill", "D-1");

        var tree = await Client.GetFromJsonAsync<List<CategoryBody>>("/api/public/tenant-a/categories");

        Assert.Empty(tree!);
        Assert.Equal(["Drill"], await Names(""));
    }

    private static async Task Assign(HttpClient admin, Guid productId, Guid categoryId) =>
        (await admin.PutAsJsonAsync($"/api/admin/products/{productId}/categories", new { categoryIds = new[] { categoryId } }))
        .EnsureSuccessStatusCode();

    private async Task<List<string>> Names(string query) =>
        (await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>($"/api/public/tenant-a/products?{query}"))!
        .Items.Select(p => p.Name).ToList();
}
