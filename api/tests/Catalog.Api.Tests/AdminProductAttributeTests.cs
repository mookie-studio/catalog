using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminProductAttributeTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Valid_values_of_every_type_round_trip()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var power = await CreateDefinition(a.Client, "Power", "Number");
        var cordless = await CreateDefinition(a.Client, "Cordless", "Boolean");
        var released = await CreateDefinition(a.Client, "Released", "Date");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);
        var sizes = await CreateDefinition(a.Client, "Sizes", "OptionList", allowsMultiple: true, options: ["S", "M", "L"]);
        await CreateDefinition(a.Client, "Unused", "Text");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/attributes", new
        {
            values = new Dictionary<string, object>
            {
                [brand.Id.ToString()] = "Acme",
                [power.Id.ToString()] = 500.5,
                [cordless.Id.ToString()] = true,
                [released.Id.ToString()] = "2026-03-01",
                [color.Id.ToString()] = color.Options[1].Id,
                [sizes.Id.ToString()] = sizes.Options.Select(o => o.Id).ToArray(),
            },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{product.Id}");
        var values = detail!.Attributes.ToDictionary(x => x.Name);
        Assert.Equal(6, values.Count);
        Assert.Equal("Acme", values["Brand"].Value.GetString());
        Assert.Equal(500.5, values["Power"].Value.GetDouble());
        Assert.True(values["Cordless"].Value.GetBoolean());
        Assert.Equal("2026-03-01", values["Released"].Value.GetString());
        Assert.Equal(color.Options[1].Id, values["Color"].Value.GetGuid());
        Assert.Equal("OptionList", values["Color"].Type);
        Assert.Equal(sizes.Options.Select(o => o.Id), values["Sizes"].Value.EnumerateArray().Select(e => e.GetGuid()));
    }

    [Fact]
    public async Task Putting_values_replaces_all_Attributes()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var power = await CreateDefinition(a.Client, "Power", "Number");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        await Put(a.Client, product.Id, new() { [brand.Id.ToString()] = "Acme", [power.Id.ToString()] = 1 });

        await Put(a.Client, product.Id, new() { [power.Id.ToString()] = 2 });

        var detail = await a.Client.GetFromJsonAsync<ProductDetailBody>($"/api/admin/products/{product.Id}");
        Assert.Equal("Power", Assert.Single(detail!.Attributes).Name);
    }

    public static TheoryData<string, string> InvalidValues => new()
    {
        { "Text", "\"\"" },
        { "Text", "42" },
        { "Text", JsonSerializer.Serialize(new string('x', 501)) },
        { "Number", "\"42\"" },
        { "Boolean", "\"true\"" },
        { "Date", "\"01/03/2026\"" },
        { "Date", "\"2026-02-30\"" },
        { "Single", "\"" + Guid.Empty + "\"" },
        { "Single", "[]" },
        { "Multi", "[]" },
        { "Multi", "\"S\"" },
        { "Multi", "DUPLICATE" },
        { "Multi", "UNKNOWN_OPTION" },
    };

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public async Task Invalid_values_return_400_keyed_by_definition_id(string kind, string rawValue)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var definition = kind switch
        {
            "Single" => await CreateDefinition(a.Client, "Color", "OptionList", options: "Red"),
            "Multi" => await CreateDefinition(a.Client, "Sizes", "OptionList", allowsMultiple: true, options: ["S", "M"]),
            _ => await CreateDefinition(a.Client, "Attr", kind),
        };
        var json = rawValue switch
        {
            "DUPLICATE" => JsonSerializer.Serialize(new[] { definition.Options[0].Id, definition.Options[0].Id }),
            "UNKNOWN_OPTION" => JsonSerializer.Serialize(new[] { definition.Options[0].Id, Guid.CreateVersion7() }),
            _ => rawValue,
        };
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await a.Client.PutAsync($"/api/admin/products/{product.Id}/attributes",
            new StringContent("{\"values\":{\"" + definition.Id + "\":" + json + "}}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(definition.Id.ToString(), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Unknown_definition_ids_and_definitions_of_another_Tenant_return_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var foreign = await CreateDefinition(b.Client, "Brand", "Text");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var unknown = await Put(a.Client, product.Id, new() { [Guid.CreateVersion7().ToString()] = "x" });
        var otherTenant = await Put(a.Client, product.Id, new() { [foreign.Id.ToString()] = "Acme" });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherTenant.StatusCode);
        Assert.Contains(foreign.Id.ToString(), (await otherTenant.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task Isolation_setting_Attributes_on_a_product_of_another_Tenant_returns_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(a.Client, "Drill", "D-1");

        var response = await Put(b.Client, product.Id, new());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Put(HttpClient client, Guid productId, Dictionary<string, object> values) =>
        client.PutAsJsonAsync($"/api/admin/products/{productId}/attributes", new { values });
}
