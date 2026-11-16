using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminAttributeDefinitionTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Theory]
    [InlineData("Text", false)]
    [InlineData("Number", false)]
    [InlineData("Boolean", false)]
    [InlineData("Date", false)]
    [InlineData("OptionList", false)]
    [InlineData("OptionList", true)]
    public async Task Each_type_can_be_created(string type, bool allowsMultiple)
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsJsonAsync("/api/admin/attribute-definitions",
            new { name = "Attr", type, isFilterable = true, allowsMultiple });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DefinitionBody>();
        Assert.Equal(type, created!.Type);
        Assert.Equal(allowsMultiple, created.AllowsMultiple);
        var list = await a.Client.GetFromJsonAsync<List<DefinitionBody>>("/api/admin/attribute-definitions");
        Assert.Equal(created.Id, Assert.Single(list!).Id);
    }

    [Fact]
    public async Task The_list_is_ordered_by_name()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateDefinition(a.Client, "Weight", "Number");
        await CreateDefinition(a.Client, "color", "Text");
        await CreateDefinition(a.Client, "Brand", "Text");

        var list = await a.Client.GetFromJsonAsync<List<DefinitionBody>>("/api/admin/attribute-definitions");

        Assert.Equal(["Brand", "color", "Weight"], list!.Select(d => d.Name));
    }

    [Fact]
    public async Task A_duplicate_name_ignoring_case_returns_409()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateDefinition(a.Client, "Color", "Text");

        var sameTenant = await a.Client.PostAsJsonAsync("/api/admin/attribute-definitions", new { name = "COLOR", type = "Text" });
        var otherTenant = await b.Client.PostAsJsonAsync("/api/admin/attribute-definitions", new { name = "Color", type = "Text" });

        Assert.Equal(HttpStatusCode.Conflict, sameTenant.StatusCode);
        Assert.Equal(HttpStatusCode.Created, otherTenant.StatusCode);
    }

    [Fact]
    public async Task Renaming_and_toggling_filterable_work_and_the_type_is_kept()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var definition = await CreateDefinition(a.Client, "Colour", "OptionList", allowsMultiple: true);

        var response = await a.Client.PutAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}",
            new { name = "Color", isFilterable = true, type = "Text", allowsMultiple = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = Assert.Single((await a.Client.GetFromJsonAsync<List<DefinitionBody>>("/api/admin/attribute-definitions"))!);
        Assert.Equal("Color", updated.Name);
        Assert.True(updated.IsFilterable);
        Assert.Equal("OptionList", updated.Type);
        Assert.True(updated.AllowsMultiple);
    }

    [Fact]
    public async Task Renaming_to_an_existing_name_returns_409()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateDefinition(a.Client, "Color", "Text");
        var size = await CreateDefinition(a.Client, "Size", "Text");

        var response = await a.Client.PutAsJsonAsync($"/api/admin/attribute-definitions/{size.Id}", new { name = "color", isFilterable = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Number")]
    [InlineData("Boolean")]
    [InlineData("Date")]
    public async Task AllowsMultiple_on_a_non_option_list_returns_400(string type)
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsJsonAsync("/api/admin/attribute-definitions",
            new { name = "Attr", type, allowsMultiple = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Isolation_definitions_of_A_are_404_and_invisible_for_B()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var definition = await CreateDefinition(a.Client, "Color", "OptionList", options: "Red");

        Assert.Empty((await b.Client.GetFromJsonAsync<List<DefinitionBody>>("/api/admin/attribute-definitions"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.PutAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}",
            new { name = "Hacked", isFilterable = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.PostAsJsonAsync($"/api/admin/attribute-definitions/{definition.Id}/options",
            new { value = "Blue" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.DeleteAsync($"/api/admin/attribute-definitions/{definition.Id}/options/{definition.Options[0].Id}?confirm=true")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Client.DeleteAsync($"/api/admin/attribute-definitions/{definition.Id}?confirm=true")).StatusCode);
        Assert.Single((await a.Client.GetFromJsonAsync<List<DefinitionBody>>("/api/admin/attribute-definitions"))!);
    }
}
