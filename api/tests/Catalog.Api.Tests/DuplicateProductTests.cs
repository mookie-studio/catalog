using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class DuplicateProductTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private sealed record CopyBody(
        Guid Id, string Name, string? Description, string Sku, string Slug, string Status, bool IsFeatured,
        List<AttributeValueBody> Attributes, List<Guid> CategoryIds, List<ImageBody> Images);

    [Fact]
    public async Task The_copy_is_a_Draft_with_copy_name_SKU_and_slug_and_the_same_Attributes_and_Categories()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var tools = await CreateCategory(a.Client, "Tools");
        var source = await CreatePublishedProduct(a.Client, "Hammer Drill", "HD-1", "Powerful");
        await a.Client.PutAsJsonAsync($"/api/admin/products/{source.Id}/attributes",
            new { values = new Dictionary<string, object> { [brand.Id.ToString()] = "Acme" } });
        await a.Client.PutAsJsonAsync($"/api/admin/products/{source.Id}/categories", new { categoryIds = new[] { tools.Id } });
        await a.Client.PutAsJsonAsync($"/api/admin/products/{source.Id}/featured", new { featured = true });

        var response = await a.Client.PostAsync($"/api/admin/products/{source.Id}/duplicate", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var copy = (await response.Content.ReadFromJsonAsync<CopyBody>())!;
        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal($"/api/admin/products/{copy.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Hammer Drill (copy)", copy.Name);
        Assert.Equal("HD-1-COPY", copy.Sku);
        Assert.Equal("hammer-drill-copy", copy.Slug);
        Assert.Equal("Draft", copy.Status);
        Assert.False(copy.IsFeatured);
        Assert.Equal("Powerful", copy.Description);
        Assert.Equal("Acme", Assert.Single(copy.Attributes).Value.GetString());
        Assert.Equal([tools.Id], copy.CategoryIds);
        Assert.Empty(copy.Images);
        var detail = await a.Client.GetFromJsonAsync<CopyBody>($"/api/admin/products/{copy.Id}");
        Assert.Equal((copy.Name, copy.Sku, copy.Slug, copy.Status), (detail!.Name, detail.Sku, detail.Slug, detail.Status));
        Assert.Equal([tools.Id], detail.CategoryIds);
    }

    [Fact]
    public async Task Duplicating_twice_yields_COPY_and_COPY_2()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var source = await CreateProduct(a.Client, "Drill", "D-1");

        var first = await Duplicate(a.Client, source.Id);
        var second = await Duplicate(a.Client, source.Id);

        Assert.Equal("D-1-COPY", first.Sku);
        Assert.Equal("D-1-COPY-2", second.Sku);
        Assert.Equal("drill-copy", first.Slug);
        Assert.Equal("drill-copy-2", second.Slug);
    }

    [Fact]
    public async Task A_name_and_SKU_at_max_length_still_produce_a_valid_unique_copy()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var sku = new string('S', 64);
        var name = new string('n', 200);
        var source = await CreateProduct(a.Client, name, sku);

        var first = await Duplicate(a.Client, source.Id);
        var second = await Duplicate(a.Client, source.Id);

        Assert.Equal(new string('S', 59) + "-COPY", first.Sku);
        Assert.Equal(new string('S', 57) + "-COPY-2", second.Sku);
        Assert.Equal(200, first.Name.Length);
        Assert.EndsWith(" (copy)", first.Name);
    }

    [Fact]
    public async Task The_copy_gets_its_own_Images_in_order_with_their_Thumbnails()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var source = await CreateProduct(a.Client, "Drill", "D-1");
        var first = await Upload(a.Client, source.Id, TestImages.Png(), "1.png");
        var second = await Upload(a.Client, source.Id, TestImages.Jpeg(), "2.jpg");

        var copy = await Duplicate(a.Client, source.Id);

        Assert.Equal([0, 1], copy.Images.Select(i => i.Position));
        Assert.All(copy.Images, i => Assert.StartsWith($"https://images.test/{a.Tenant.Id}/{copy.Id}/", i.Url));
        Assert.EndsWith(".png", copy.Images[0].Url);
        Assert.EndsWith(".jpg", copy.Images[1].Url);
        Assert.All(copy.Images, i => Assert.EndsWith("-thumb.webp", i.ThumbnailUrl));
        Assert.Equal(
            Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(first.Url)].Content,
            Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(copy.Images[0].Url)].Content);
        Assert.Equal(
            Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(second.ThumbnailUrl)].Content,
            Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(copy.Images[1].ThumbnailUrl)].Content);
        Assert.Equal(8, Factory.ImageStorage.Files.Count);
    }

    [Fact]
    public async Task Deleting_an_Image_of_the_copy_leaves_the_source_untouched()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var source = await CreateProduct(a.Client, "Drill", "D-1");
        var original = await Upload(a.Client, source.Id, TestImages.Png(), "1.png");
        var copy = await Duplicate(a.Client, source.Id);

        await a.Client.DeleteAsync($"/api/admin/products/{copy.Id}/images/{copy.Images[0].Id}");

        var sourceDetail = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{source.Id}");
        Assert.Equal(original, Assert.Single(sourceDetail!.Images));
        Assert.True(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(original.Url)));
        Assert.True(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(original.ThumbnailUrl)));
        Assert.Equal(2, Factory.ImageStorage.Files.Count);
    }

    [Fact]
    public async Task Isolation_duplicating_another_Tenants_product_returns_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var theirs = await CreateProduct(b.Client, "Drill", "D-1");

        var response = await a.Client.PostAsync($"/api/admin/products/{theirs.Id}/duplicate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var list = await b.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products");
        Assert.Single(list!.Items);
    }

    private static async Task<CopyBody> Duplicate(HttpClient client, Guid id)
    {
        var response = await client.PostAsync($"/api/admin/products/{id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CopyBody>())!;
    }

    private static async Task<ImageBody> Upload(HttpClient client, Guid productId, byte[] content, string fileName)
    {
        var response = await client.PostAsync($"/api/admin/products/{productId}/images", TestImages.Form(content, fileName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ImageBody>())!;
    }
}
