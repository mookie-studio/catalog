using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PublicCatalogTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task The_Tenant_is_resolved_by_slug_anonymously()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var tenant = await Client.GetFromJsonAsync<TenantInfo>("/api/public/tenant-a");

        Assert.Equal(new TenantInfo(a.Tenant.Name, "tenant-a"), tenant);
    }

    [Fact]
    public async Task Only_Published_products_appear_ordered_by_name()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Saw", "S-1");
        await CreatePublishedProduct(a.Client, "Drill", "D-1");
        await CreateProduct(a.Client, "Draft", "X-1");
        var archived = await CreatePublishedProduct(a.Client, "Archived", "A-1");
        await a.Client.PostAsJsonAsync($"/api/admin/products/{archived.Id}/status", new { status = "Archived" });

        var page = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");

        Assert.Equal(["Drill", "Saw"], page!.Items.Select(p => p.Name));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task A_Deactivated_tenant_and_an_unknown_slug_return_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Drill", "D-1");
        await PlatformAdminClient().PostAsync($"/api/platform/tenants/{a.Tenant.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/nobody/products")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/nobody/categories")).StatusCode);
    }

    [Fact]
    public async Task Products_of_another_Tenant_never_appear()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreatePublishedProduct(a.Client, "Mine", "M-1");
        await CreatePublishedProduct(b.Client, "Theirs", "T-1");

        var page = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");

        Assert.Equal(["Mine"], page!.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task MainImageUrl_is_the_Thumbnail_of_the_first_Image_or_null()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var withImages = await CreatePublishedProduct(a.Client, "A with images", "A-1");
        await CreatePublishedProduct(a.Client, "B without images", "B-1");
        var first = await (await a.Client.PostAsync($"/api/admin/products/{withImages.Id}/images", TestImages.Form(TestImages.Png(), "1.png")))
            .Content.ReadFromJsonAsync<ImageBody>();
        var second = await (await a.Client.PostAsync($"/api/admin/products/{withImages.Id}/images", TestImages.Form(TestImages.Jpeg(), "2.jpg")))
            .Content.ReadFromJsonAsync<ImageBody>();

        var page = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        Assert.Equal(first!.ThumbnailUrl, page!.Items[0].MainImageUrl);
        Assert.Null(page.Items[1].MainImageUrl);

        await a.Client.PutAsJsonAsync($"/api/admin/products/{withImages.Id}/images/order", new { imageIds = new[] { second!.Id, first.Id } });
        page = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        Assert.Equal(second.ThumbnailUrl, page!.Items[0].MainImageUrl);
    }

    [Fact]
    public async Task The_list_is_paged()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        for (var i = 1; i <= 7; i++)
        {
            await CreatePublishedProduct(a.Client, $"Product {i}", $"P-{i}");
        }

        var page = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products?page=2&pageSize=3");

        Assert.Equal(["Product 4", "Product 5", "Product 6"], page!.Items.Select(p => p.Name));
        Assert.Equal(7, page.TotalCount);
    }

    private sealed record TenantInfo(string Name, string Slug);
}
