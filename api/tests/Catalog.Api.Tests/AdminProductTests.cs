using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminProductTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Creating_a_product_generates_the_slug_and_starts_as_Draft()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsJsonAsync("/api/admin/products", new { name = "Taladro Percutor 500W", sku = "TP-500" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductBody>();
        Assert.Equal("taladro-percutor-500w", product!.Slug);
        Assert.Equal("Draft", product.Status);
        Assert.Equal(product, await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}"));
    }

    [Theory]
    [InlineData("Cañón Ñandú — Edición Única!", "canon-nandu-edicion-unica")]
    [InlineData("  --Spaces   and___symbols--  ", "spaces-and-symbols")]
    public async Task Slug_generation_removes_diacritics_and_symbols(string name, string expected)
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var product = await CreateProduct(a.Client, name, "SKU-1");

        Assert.Equal(expected, product.Slug);
    }

    [Fact]
    public async Task Long_names_are_cut_to_80_characters()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var product = await CreateProduct(a.Client, new string('a', 150), "SKU-1");

        Assert.Equal(80, product.Slug.Length);
    }

    [Fact]
    public async Task Repeated_names_get_disambiguated_slugs()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var first = await CreateProduct(a.Client, "Hammer", "H-1");
        var second = await CreateProduct(a.Client, "Hammer", "H-2");
        var third = await CreateProduct(a.Client, "Hammer", "H-3");

        Assert.Equal(["hammer", "hammer-2", "hammer-3"], new[] { first.Slug, second.Slug, third.Slug });
    }

    [Fact]
    public async Task The_same_SKU_is_accepted_in_two_Tenants_but_rejected_within_one()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateProduct(a.Client, "Hammer", "SKU-1");

        var otherTenant = await b.Client.PostAsJsonAsync("/api/admin/products", new { name = "Hammer", sku = "SKU-1" });
        var sameTenant = await a.Client.PostAsJsonAsync("/api/admin/products", new { name = "Other", sku = "SKU-1" });

        Assert.Equal(HttpStatusCode.Created, otherTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sameTenant.StatusCode);
    }

    [Fact]
    public async Task Missing_name_or_SKU_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsJsonAsync("/api/admin/products", new { name = " ", sku = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("name", problem!.Errors.Keys);
        Assert.Contains("sku", problem.Errors.Keys);
    }

    [Fact]
    public async Task Isolation_an_Admin_of_B_gets_404_for_a_product_of_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "Hammer", "SKU-1", "A hammer");

        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.GetAsync($"/api/admin/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}",
            new { name = "Stolen", description = "x", sku = "S", slug = "stolen" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status",
            new { status = "Archived" })).StatusCode);
        Assert.Equal("Draft", (await a.Client.GetFromJsonAsync<ProductBody>($"/api/admin/products/{product.Id}"))!.Status);
    }
}
