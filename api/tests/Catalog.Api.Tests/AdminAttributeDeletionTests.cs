using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminAttributeDeletionTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Deleting_an_unused_Option_or_definition_returns_204()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);

        var option = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{color.Id}/options/{color.Options[0].Id}");
        Assert.Equal(HttpStatusCode.NoContent, option.StatusCode);
        Assert.Equal(["Blue"], (await Definitions(a.Client)).Single().Options.Select(o => o.Value));

        var definition = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{color.Id}");
        Assert.Equal(HttpStatusCode.NoContent, definition.StatusCode);
        Assert.Empty(await Definitions(a.Client));
    }

    [Fact]
    public async Task Deleting_a_used_definition_without_confirmation_returns_409_with_usageCount()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        foreach (var sku in new[] { "A", "B" })
        {
            var p = await CreateProduct(a.Client, sku, sku);
            await SetAttributes(a.Client, p.Id, new() { [brand.Id.ToString()] = "Acme" });
        }

        await CreateProduct(a.Client, "Unrelated", "C");

        var response = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{brand.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(2, ((System.Text.Json.JsonElement)problem!.Extensions["usageCount"]!).GetInt32());
        Assert.Single(await Definitions(a.Client));
    }

    [Fact]
    public async Task Deleting_a_used_definition_with_confirmation_removes_the_values()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var power = await CreateDefinition(a.Client, "Power", "Number");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        await SetAttributes(a.Client, product.Id, new() { [brand.Id.ToString()] = "Acme", [power.Id.ToString()] = 500 });

        var response = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{brand.Id}?confirm=true");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var detail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{product.Id}");
        Assert.Equal("Power", Assert.Single(detail!.Attributes).Name);
    }

    [Fact]
    public async Task Deleting_a_used_Option_counts_and_removes_only_that_Option()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var sizes = await CreateDefinition(a.Client, "Sizes", "OptionList", allowsMultiple: true, options: ["S", "M"]);
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);
        var both = await CreateProduct(a.Client, "Both", "B-1");
        var onlyS = await CreateProduct(a.Client, "Only S", "S-1");
        var onlyM = await CreateProduct(a.Client, "Only M", "M-1");
        await SetAttributes(a.Client, both.Id, new()
        {
            [sizes.Id.ToString()] = new[] { sizes.Options[0].Id, sizes.Options[1].Id },
            [color.Id.ToString()] = color.Options[0].Id,
        });
        await SetAttributes(a.Client, onlyS.Id, new() { [sizes.Id.ToString()] = new[] { sizes.Options[0].Id } });
        await SetAttributes(a.Client, onlyM.Id, new() { [sizes.Id.ToString()] = new[] { sizes.Options[1].Id } });

        var unconfirmed = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{sizes.Id}/options/{sizes.Options[0].Id}");
        Assert.Equal(HttpStatusCode.Conflict, unconfirmed.StatusCode);
        var problem = await unconfirmed.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(2, ((System.Text.Json.JsonElement)problem!.Extensions["usageCount"]!).GetInt32());

        var confirmed = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{sizes.Id}/options/{sizes.Options[0].Id}?confirm=true");
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);

        var bothDetail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{both.Id}");
        var bothSizes = bothDetail!.Attributes.Single(x => x.Name == "Sizes");
        Assert.Equal([sizes.Options[1].Id], bothSizes.Value.EnumerateArray().Select(e => e.GetGuid()));
        Assert.Contains(bothDetail.Attributes, x => x.Name == "Color");

        var onlySDetail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{onlyS.Id}");
        Assert.Empty(onlySDetail!.Attributes);

        var onlyMDetail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{onlyM.Id}");
        Assert.Single(onlyMDetail!.Attributes);
    }

    [Fact]
    public async Task Deleting_a_used_single_select_Option_with_confirmation_drops_the_key()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        await SetAttributes(a.Client, product.Id, new() { [color.Id.ToString()] = color.Options[0].Id });

        var unusedOption = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{color.Id}/options/{color.Options[1].Id}");
        var usedOption = await a.Client.DeleteAsync($"/api/admin/attribute-definitions/{color.Id}/options/{color.Options[0].Id}?confirm=true");

        Assert.Equal(HttpStatusCode.NoContent, unusedOption.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, usedOption.StatusCode);
        Assert.Empty((await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{product.Id}"))!.Attributes);
    }

    private static async Task<List<DefinitionBody>> Definitions(HttpClient client) =>
        (await client.GetFromJsonAsync<PagedBody<DefinitionBody>>("/api/admin/attribute-definitions"))!.Items;

    private static async Task SetAttributes(HttpClient client, Guid productId, Dictionary<string, object> values) =>
        (await client.PutAsJsonAsync($"/api/admin/products/{productId}/attributes", new { values })).EnsureSuccessStatusCode();
}
