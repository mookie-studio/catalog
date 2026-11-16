using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminProductEditTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Renaming_keeps_the_existing_slug_and_updates_UpdatedAt()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1");

        var response = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Big Hammer", description = "Heavy", sku = "H-1", slug = product.Slug });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}");
        Assert.Equal("Big Hammer", updated!.Name);
        Assert.Equal("Heavy", updated.Description);
        Assert.Equal("hammer", updated.Slug);
        Assert.True(updated.UpdatedAt > product.UpdatedAt);
    }

    [Fact]
    public async Task The_slug_changes_when_a_different_one_is_sent()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1");

        await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Hammer", description = (string?)null, sku = "H-1", slug = "classic-hammer" });

        Assert.Equal("classic-hammer", (await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}"))!.Slug);
    }

    [Fact]
    public async Task A_slug_or_SKU_in_use_returns_409_and_an_invalid_slug_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateProduct(a.Client, "Saw", "S-1");
        var product = await CreateProduct(a.Client, "Hammer", "H-1");

        var slugTaken = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Hammer", sku = "H-1", slug = "saw" });
        var skuTaken = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Hammer", sku = "S-1", slug = "hammer" });
        var invalid = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Hammer", sku = "H-1", slug = "Not A Slug" });

        Assert.Equal(HttpStatusCode.Conflict, slugTaken.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, skuTaken.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("slug", (await invalid.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task Clearing_the_description_of_a_Published_product_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1", "A hammer");
        await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = "Published" });

        var response = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Hammer", description = " ", sku = "H-1", slug = "hammer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("description", (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task An_Archived_product_can_be_edited()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1");
        await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = "Archived" });

        var response = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Old Hammer", sku = "H-1", slug = "hammer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}");
        Assert.Equal("Old Hammer", updated!.Name);
        Assert.Equal("Archived", updated.Status);
    }

    [Fact]
    public async Task Publishing_requires_a_description()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var withDescription = await CreateProduct(a.Client, "Hammer", "H-1", "A hammer");
        var withoutDescription = await CreateProduct(a.Client, "Saw", "S-1");

        var ok = await a.Client.PostAsJsonAsync($"/api/admin/products/{withDescription.Id}/status", new { status = "Published" });
        var fails = await a.Client.PostAsJsonAsync($"/api/admin/products/{withoutDescription.Id}/status", new { status = "Published" });

        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.Equal("Published", (await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{withDescription.Id}"))!.Status);
        Assert.Equal(HttpStatusCode.BadRequest, fails.StatusCode);
        Assert.Contains("description", (await fails.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal("Draft", (await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{withoutDescription.Id}"))!.Status);
    }

    [Theory]
    [InlineData("Published", "Draft")]
    [InlineData("Published", "Archived")]
    [InlineData("Archived", "Published")]
    [InlineData("Archived", "Draft")]
    public async Task Any_transition_is_allowed(string from, string to)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1", "A hammer");
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = from })).EnsureSuccessStatusCode();

        var response = await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = to });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(to, (await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}"))!.Status);
    }

    [Fact]
    public async Task An_unknown_status_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer", "H-1", "A hammer");

        var response = await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = "Deleted" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
