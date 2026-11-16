using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminProductCategoryTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task A_product_can_be_in_several_Categories_or_none()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var offers = await CreateCategory(a.Client, "Offers");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        Assert.Empty((await Detail(a.Client, product.Id)).CategoryIds);

        var several = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = new[] { tools.Id, offers.Id } });
        Assert.Equal(HttpStatusCode.OK, several.StatusCode);
        Assert.Equal(new[] { tools.Id, offers.Id }.Order(), (await Detail(a.Client, product.Id)).CategoryIds.Order());

        await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = Array.Empty<Guid>() });
        Assert.Empty((await Detail(a.Client, product.Id)).CategoryIds);
    }

    [Fact]
    public async Task The_category_filter_returns_only_products_directly_in_it()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var drills = await CreateCategory(a.Client, "Drills", tools.Id);
        var inTools = await CreateProduct(a.Client, "Hammer", "H-1");
        var inDrills = await CreateProduct(a.Client, "Drill", "D-1");
        await CreateProduct(a.Client, "Loose", "L-1");
        await a.Client.PutAsJsonAsync($"/api/admin/products/{inTools.Id}/categories", new { categoryIds = new[] { tools.Id } });
        await a.Client.PutAsJsonAsync($"/api/admin/products/{inDrills.Id}/categories", new { categoryIds = new[] { drills.Id } });

        var list = await a.Client.GetFromJsonAsync<PagedBody<ProductListItem>>($"/api/admin/products?categoryId={tools.Id}");

        Assert.Equal([inTools.Id], list!.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Unknown_Category_ids_or_ids_from_another_Tenant_return_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var foreign = await CreateCategory(b.Client, "Theirs");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var otherTenant = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = new[] { foreign.Id } });
        var unknown = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = new[] { Guid.CreateVersion7() } });

        Assert.Equal(HttpStatusCode.BadRequest, otherTenant.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }

    [Fact]
    public async Task Isolation_assigning_Categories_to_a_product_of_another_Tenant_returns_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await b.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<ProductCategoriesBody> Detail(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<ProductCategoriesBody>($"/api/admin/products/{id}"))!;
}
