using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PublicFilterTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Filters_lists_only_Filterable_attributes_with_their_options()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", isFilterable: true, options: ["Red", "Blue"]);
        await CreateDefinition(a.Client, "Internal", "Text");
        await CreateDefinition(a.Client, "Brand", "Text", isFilterable: true);

        var filters = await Client.GetFromJsonAsync<List<PublicFilterBody>>("/api/public/tenant-a/filters");

        Assert.Equal(["Brand", "Color"], filters!.Select(f => f.Name));
        Assert.Null(filters[0].Options);
        Assert.Equal(["Red", "Blue"], filters[1].Options!.Select(o => o.Value));
        Assert.Equal(color.Id, filters[1].DefinitionId);
    }

    [Fact]
    public async Task Option_filters_OR_within_a_definition_and_AND_across_definitions()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", isFilterable: true, options: ["Red", "Blue", "Green"]);
        var sizes = await CreateDefinition(a.Client, "Sizes", "OptionList", isFilterable: true, allowsMultiple: true, options: ["S", "M", "L"]);
        var (red, blue, green) = (color.Options[0].Id, color.Options[1].Id, color.Options[2].Id);
        var (s, m, l) = (sizes.Options[0].Id, sizes.Options[1].Id, sizes.Options[2].Id);
        await Published(a.Client, "Red S M", new() { [color.Id.ToString()] = red, [sizes.Id.ToString()] = new[] { s, m } });
        await Published(a.Client, "Blue L", new() { [color.Id.ToString()] = blue, [sizes.Id.ToString()] = new[] { l } });
        await Published(a.Client, "Green M", new() { [color.Id.ToString()] = green, [sizes.Id.ToString()] = new[] { m } });
        await Published(a.Client, "Nothing", new());

        Assert.Equal(["Red S M"], await Names($"attr.{color.Id}={red}"));
        Assert.Equal(["Blue L", "Red S M"], await Names($"attr.{color.Id}={red}&attr.{color.Id}={blue}"));
        Assert.Equal(["Green M", "Red S M"], await Names($"attr.{sizes.Id}={m}"));
        Assert.Equal(["Green M"], await Names($"attr.{sizes.Id}={m}&attr.{color.Id}={green}&attr.{color.Id}={blue}"));
        Assert.Empty(await Names($"attr.{sizes.Id}={l}&attr.{color.Id}={red}"));
    }

    [Fact]
    public async Task Text_filters_are_case_insensitive_exact_and_scalars_are_exact()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text", isFilterable: true);
        var power = await CreateDefinition(a.Client, "Power", "Number", isFilterable: true);
        var cordless = await CreateDefinition(a.Client, "Cordless", "Boolean", isFilterable: true);
        var released = await CreateDefinition(a.Client, "Released", "Date", isFilterable: true);
        await Published(a.Client, "Acme drill", new()
        {
            [brand.Id.ToString()] = "Acme", [power.Id.ToString()] = 500.5, [cordless.Id.ToString()] = true,
            [released.Id.ToString()] = "2026-03-01",
        });
        await Published(a.Client, "Acme Pro saw", new()
        {
            [brand.Id.ToString()] = "Acme Pro", [power.Id.ToString()] = 1200, [cordless.Id.ToString()] = false,
            [released.Id.ToString()] = "2025-01-15",
        });

        Assert.Equal(["Acme drill"], await Names($"attr.{brand.Id}=aCmE"));
        Assert.Equal(["Acme drill"], await Names($"attr.{power.Id}=500.50"));
        Assert.Equal(["Acme Pro saw"], await Names($"attr.{power.Id}=1200"));
        Assert.Equal(["Acme Pro saw"], await Names($"attr.{cordless.Id}=false"));
        Assert.Equal(["Acme drill"], await Names($"attr.{released.Id}=2026-03-01"));
    }

    [Fact]
    public async Task Filters_combine_with_search_and_only_return_Published_products()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", isFilterable: true, options: ["Red"]);
        var red = color.Options[0].Id.ToString();
        await Published(a.Client, "Taladro rojo", new() { [color.Id.ToString()] = red });
        await Published(a.Client, "Sierra roja", new() { [color.Id.ToString()] = red });
        var draft = await CreateProduct(a.Client, "Taladro borrador", "D-1", "x");
        await a.Client.PutAsJsonAsync($"/api/admin/products/{draft.Id}/attributes", new { values = new Dictionary<string, object> { [color.Id.ToString()] = red } });

        Assert.Equal(["Taladro rojo"], await Names($"q=taladro&attr.{color.Id}={red}"));
        Assert.Equal(["Sierra roja", "Taladro rojo"], await Names($"attr.{color.Id}={red}"));
    }

    [Fact]
    public async Task A_non_filterable_unknown_or_malformed_filter_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var hidden = await CreateDefinition(a.Client, "Internal", "Text");
        var power = await CreateDefinition(a.Client, "Power", "Number", isFilterable: true);

        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync($"/api/public/tenant-a/products?attr.{hidden.Id}=x")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync($"/api/public/tenant-a/products?attr.{Guid.CreateVersion7()}=x")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync($"/api/public/tenant-a/products?attr.{power.Id}=lots")).StatusCode);
    }

    private async Task Published(HttpClient admin, string name, Dictionary<string, object> values)
    {
        var product = await CreatePublishedProduct(admin, name, name.Replace(' ', '-'));
        (await admin.PutAsJsonAsync($"/api/admin/products/{product.Id}/attributes", new { values })).EnsureSuccessStatusCode();
    }

    private async Task<List<string>> Names(string query) =>
        (await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>($"/api/public/tenant-a/products?{query}"))!
        .Items.Select(p => p.Name).ToList();
}
