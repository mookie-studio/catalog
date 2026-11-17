using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace Catalog.Api.Tests;

public sealed class ThumbnailTests(CatalogApiFactory factory) : ApiTest(factory)
{
    public static TheoryData<string, string> Formats => new()
    {
        { "png", "1.png" },
        { "jpeg", "1.jpg" },
        { "webp", "1.webp" },
    };

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task Uploading_a_wide_image_stores_a_WebP_Thumbnail_at_most_480_px_wide(string format, string fileName)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var original = format switch
        {
            "png" => TestImages.Png(1200, 900),
            "jpeg" => TestImages.Jpeg(1200, 900),
            _ => TestImages.WebP(1200, 900),
        };

        var image = await Upload(a.Client, product.Id, original, fileName);

        Assert.Equal($"https://images.test/{a.Tenant.Id}/{product.Id}/{image.Id}-thumb.webp", image.ThumbnailUrl);
        var thumbnail = Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(image.ThumbnailUrl)];
        Assert.Equal("image/webp", thumbnail.ContentType);
        Assert.Equal(SKEncodedImageFormat.Webp, TestImages.FormatOf(thumbnail.Content));
        Assert.Equal((480, 360), TestImages.SizeOf(thumbnail.Content));
        Assert.Equal(original, Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(image.Url)].Content);
    }

    [Fact]
    public async Task A_narrow_image_is_not_upscaled()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var image = await Upload(a.Client, product.Id, TestImages.Png(300, 200), "small.png");

        var thumbnail = Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(image.ThumbnailUrl)];
        Assert.Equal(SKEncodedImageFormat.Webp, TestImages.FormatOf(thumbnail.Content));
        Assert.Equal((300, 200), TestImages.SizeOf(thumbnail.Content));
    }

    [Fact]
    public async Task Deleting_an_Image_removes_the_original_and_the_Thumbnail()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var image = await Upload(a.Client, product.Id, TestImages.Jpeg(1200, 900), "1.jpg");

        var response = await a.Client.DeleteAsync($"/api/admin/products/{product.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Factory.ImageStorage.Files);
    }

    [Fact]
    public async Task A_corrupt_file_with_a_valid_header_returns_400_on_file()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await a.Client.PostAsync(
            $"/api/admin/products/{product.Id}/images", TestImages.Form(TestImages.CorruptPng(), "broken.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["file"], (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Empty(Factory.ImageStorage.Files);
    }

    [Fact]
    public async Task Listings_and_detail_use_the_Thumbnail_and_keep_the_original_in_url()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreatePublishedProduct(a.Client, "Drill", "D-1");
        var image = await Upload(a.Client, product.Id, TestImages.Jpeg(1200, 900), "1.jpg");

        var list = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        var detail = await Client.GetFromJsonAsync<PublicProductBody>("/api/public/tenant-a/products/drill");

        Assert.EndsWith("-thumb.webp", image.ThumbnailUrl);
        Assert.Equal(image.ThumbnailUrl, Assert.Single(list!.Items).MainImageUrl);
        Assert.Equal(new PublicImageBody(image.Url, image.ThumbnailUrl), Assert.Single(detail!.Images));
    }

    [Fact]
    public async Task An_Image_without_a_Thumbnail_falls_back_to_the_original_everywhere()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreatePublishedProduct(a.Client, "Drill", "D-1");
        Factory.ImageStorage.FailSaving = key => key.EndsWith("-thumb.webp", StringComparison.Ordinal);

        var image = await Upload(a.Client, product.Id, TestImages.Jpeg(1200, 900), "1.jpg");

        Assert.Equal(image.Url, image.ThumbnailUrl);
        var admin = await a.Client.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{product.Id}");
        var list = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/tenant-a/products");
        var detail = await Client.GetFromJsonAsync<PublicProductBody>("/api/public/tenant-a/products/drill");
        Assert.Equal(image.Url, Assert.Single(admin!.Images).ThumbnailUrl);
        Assert.Equal(image.Url, Assert.Single(list!.Items).MainImageUrl);
        Assert.Equal(new PublicImageBody(image.Url, image.Url), Assert.Single(detail!.Images));

        var response = await a.Client.DeleteAsync($"/api/admin/products/{product.Id}/images/{image.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Factory.ImageStorage.Files);
    }

    [Fact]
    public async Task Isolation_another_Tenant_cannot_reach_the_Thumbnail_through_image_endpoints()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        var image = await Upload(a.Client, product.Id, TestImages.Png(), "1.png");

        var response = await b.Client.DeleteAsync($"/api/admin/products/{product.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(Factory.ImageStorage.Files.ContainsKey(InMemoryImageStorage.KeyOf(image.ThumbnailUrl)));
    }

    private static async Task<ImageBody> Upload(HttpClient client, Guid productId, byte[] content, string fileName)
    {
        var response = await client.PostAsync($"/api/admin/products/{productId}/images", TestImages.Form(content, fileName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ImageBody>())!;
    }
}
