using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PublicProductDetailTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task A_Published_product_returns_full_detail_with_images_in_order_and_option_texts()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);
        var sizes = await CreateDefinition(a.Client, "Sizes", "OptionList", allowsMultiple: true, options: ["S", "M", "L"]);
        var product = await CreatePublishedProduct(a.Client, "Hammer Drill", "HD-1", "Powerful");
        await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/attributes", new
        {
            values = new Dictionary<string, object>
            {
                [brand.Id.ToString()] = "Acme",
                [color.Id.ToString()] = color.Options[1].Id,
                [sizes.Id.ToString()] = new[] { sizes.Options[2].Id, sizes.Options[0].Id },
            },
        });
        var first = await (await a.Client.PostAsync($"/api/admin/products/{product.Id}/images", TestImages.Form(TestImages.Png(), "1.png")))
            .Content.ReadFromJsonAsync<ImageBody>();
        var second = await (await a.Client.PostAsync($"/api/admin/products/{product.Id}/images", TestImages.Form(TestImages.Jpeg(), "2.jpg")))
            .Content.ReadFromJsonAsync<ImageBody>();

        var detail = await Client.GetFromJsonAsync<PublicProductBody>("/api/public/tenant-a/products/hammer-drill");

        Assert.Equal("hammer-drill", detail!.Slug);
        Assert.Equal("Hammer Drill", detail.Name);
        Assert.Equal("Powerful", detail.Description);
        Assert.Equal([new PublicImageBody(first!.Url, first.ThumbnailUrl), new PublicImageBody(second!.Url, second.ThumbnailUrl)], detail.Images);
        var attributes = detail.Attributes.ToDictionary(x => x.Name);
        Assert.Equal("Acme", attributes["Brand"].Value.GetString());
        Assert.Equal("Blue", attributes["Color"].Value.GetString());
        Assert.Equal(["S", "L"], attributes["Sizes"].Value.EnumerateArray().Select(v => v.GetString()));
    }

    [Fact]
    public async Task Draft_Archived_and_unknown_products_return_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateProduct(a.Client, "Draft", "D-1", "x");
        var archived = await CreatePublishedProduct(a.Client, "Archived", "A-1");
        await a.Client.PostAsJsonAsync($"/api/admin/products/{archived.Id}/status", new { status = "Archived" });

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/draft")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/archived")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/nothing")).StatusCode);
    }

    [Fact]
    public async Task A_product_of_another_Tenant_returns_404_under_this_Tenant_slug()
    {
        await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreatePublishedProduct(b.Client, "Theirs", "T-1");

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/theirs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/public/tenant-b/products/theirs")).StatusCode);
    }
}
