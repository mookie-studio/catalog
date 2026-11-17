using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminProductImageTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Uploading_a_valid_PNG_stores_it_and_returns_its_URL()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await a.Client.PostAsync($"/api/admin/products/{product.Id}/images", TestImages.Form(TestImages.Png(), "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var image = await response.Content.ReadFromJsonAsync<ImageBody>();
        Assert.Equal(0, image!.Position);
        Assert.Equal($"https://images.test/{a.Tenant.Id}/{product.Id}/{image.Id}.png", image.Url);
        var stored = Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(image.Url)];
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal(TestImages.Png(), stored.Content);

        var detail = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{product.Id}");
        Assert.Equal(image, Assert.Single(detail!.Images));
    }

    [Fact]
    public async Task JPEG_and_WebP_are_accepted_by_content_and_appended_last()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var jpeg = await Upload(a.Client, product.Id, TestImages.Jpeg(), "a.bin");
        var webp = await Upload(a.Client, product.Id, TestImages.WebP(), "b");

        Assert.Equal(HttpStatusCode.Created, jpeg.StatusCode);
        Assert.Equal(HttpStatusCode.Created, webp.StatusCode);
        var detail = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{product.Id}");
        Assert.Equal([0, 1], detail!.Images.Select(i => i.Position));
        Assert.EndsWith(".jpg", detail.Images[0].Url);
        Assert.EndsWith(".webp", detail.Images[1].Url);
    }

    [Fact]
    public async Task A_GIF_a_fake_JPEG_and_a_file_over_2MB_are_rejected()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var gif = await Upload(a.Client, product.Id, TestImages.Gif(), "anim.gif");
        var fakeJpeg = await Upload(a.Client, product.Id, "just some text pretending"u8.ToArray(), "photo.jpg");
        var tooBig = await Upload(a.Client, product.Id, TestImages.OversizedPng((int)(2.1 * 1024 * 1024)), "big.png");

        Assert.Equal(HttpStatusCode.BadRequest, gif.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, fakeJpeg.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);
        Assert.Empty(Factory.ImageStorage.Files);
    }

    [Fact]
    public async Task A_sixth_image_is_rejected()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await Upload(a.Client, product.Id, TestImages.Png(), $"{i}.png")).StatusCode);
        }

        var sixth = await Upload(a.Client, product.Id, TestImages.Png(), "6.png");

        Assert.Equal(HttpStatusCode.BadRequest, sixth.StatusCode);
        Assert.Equal(10, Factory.ImageStorage.Files.Count); // Five originals and their Thumbnails.
    }

    [Fact]
    public async Task Reordering_changes_the_Main_image()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var first = await (await Upload(a.Client, product.Id, TestImages.Png(), "1.png")).Content.ReadFromJsonAsync<ImageBody>();
        var second = await (await Upload(a.Client, product.Id, TestImages.Jpeg(), "2.jpg")).Content.ReadFromJsonAsync<ImageBody>();

        var response = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/images/order",
            new { imageIds = new[] { second!.Id, first!.Id } });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detail = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{product.Id}");
        Assert.Equal([second.Id, first.Id], detail!.Images.Select(i => i.Id));
    }

    [Fact]
    public async Task Reordering_with_a_missing_duplicate_or_foreign_id_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var other = await CreateProduct(a.Client, "Saw", "S-1");
        var first = await (await Upload(a.Client, product.Id, TestImages.Png(), "1.png")).Content.ReadFromJsonAsync<ImageBody>();
        await Upload(a.Client, product.Id, TestImages.Png(), "2.png");
        var foreign = await (await Upload(a.Client, other.Id, TestImages.Png(), "3.png")).Content.ReadFromJsonAsync<ImageBody>();

        var missing = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/images/order", new { imageIds = new[] { first!.Id } });
        var duplicate = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/images/order", new { imageIds = new[] { first.Id, first.Id } });
        var withForeign = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/images/order", new { imageIds = new[] { first.Id, foreign!.Id } });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, withForeign.StatusCode);
    }

    [Fact]
    public async Task Deleting_removes_the_file_and_compacts_positions()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var first = await (await Upload(a.Client, product.Id, TestImages.Png(), "1.png")).Content.ReadFromJsonAsync<ImageBody>();
        var second = await (await Upload(a.Client, product.Id, TestImages.Png(), "2.png")).Content.ReadFromJsonAsync<ImageBody>();

        var response = await a.Client.DeleteAsync($"/api/admin/products/{product.Id}/images/{first!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(first.Url)));
        Assert.False(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(first.ThumbnailUrl)));
        Assert.True(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(second!.ThumbnailUrl)));
        var detail = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{product.Id}");
        var remaining = Assert.Single(detail!.Images);
        Assert.Equal(second.Id, remaining.Id);
        Assert.Equal(0, remaining.Position);
    }

    [Fact]
    public async Task Isolation_image_endpoints_return_404_across_Tenants()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var image = await (await Upload(a.Client, product.Id, TestImages.Png(), "1.png")).Content.ReadFromJsonAsync<ImageBody>();

        Assert.Equal(HttpStatusCode.NotFound, (await Upload(b.Client, product.Id, TestImages.Png(), "x.png")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/images/order",
            new { imageIds = new[] { image!.Id } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.DeleteAsync($"/api/admin/products/{product.Id}/images/{image.Id}")).StatusCode);
        Assert.Equal(2, Factory.ImageStorage.Files.Count); // The original and its Thumbnail.
    }

    private static Task<HttpResponseMessage> Upload(HttpClient client, Guid productId, byte[] content, string fileName) =>
        client.PostAsync($"/api/admin/products/{productId}/images", TestImages.Form(content, fileName));
}
