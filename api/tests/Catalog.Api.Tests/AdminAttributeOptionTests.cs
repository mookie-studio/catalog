using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminAttributeOptionTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Options_are_returned_in_insertion_order()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateDefinition(a.Client, "Size", "OptionList", allowsMultiple: true, options: ["S", "M", "L", "XL"]);

        var definition = Assert.Single((await a.Client.GetFromJsonAsync<PagedBody<DefinitionBody>>("/api/admin/attribute-definitions"))!.Items);

        Assert.Equal(["S", "M", "L", "XL"], definition.Options.Select(o => o.Value));
        Assert.Equal([0, 1, 2, 3], definition.Options.Select(o => o.Position));
    }

    [Fact]
    public async Task A_duplicate_value_ignoring_case_returns_409()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var definition = await CreateDefinition(a.Client, "Color", "OptionList", options: "Red");

        var response = await a.Client.PostAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}/options", new { value = "RED" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Adding_an_Option_to_a_Text_definition_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var definition = await CreateDefinition(a.Client, "Brand", "Text");

        var response = await a.Client.PostAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}/options", new { value = "Acme" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_value_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var definition = await CreateDefinition(a.Client, "Color", "OptionList");

        var response = await a.Client.PostAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}/options", new { value = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
