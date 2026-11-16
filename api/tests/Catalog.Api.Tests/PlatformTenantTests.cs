using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class PlatformTenantTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Creating_a_Tenant_returns_201_and_it_appears_in_the_list()
    {
        var client = PlatformAdminClient();

        var response = await client.PostAsJsonAsync("/api/platform/tenants", new { name = "Acme Hardware", slug = "acme-hardware" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TenantBody>();
        Assert.Equal("Acme Hardware", created!.Name);
        Assert.Equal("acme-hardware", created.Slug);
        Assert.True(created.IsActive);
        Assert.NotEqual(Guid.Empty, created.Id);

        var list = await client.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants");
        Assert.Equal(1, list!.TotalCount);
        Assert.Equal(created, Assert.Single(list.Items));
    }

    [Fact]
    public async Task A_duplicate_slug_returns_409()
    {
        var client = PlatformAdminClient();
        await client.PostAsJsonAsync("/api/platform/tenants", new { name = "First", slug = "taken" });

        var response = await client.PostAsJsonAsync("/api/platform/tenants", new { name = "Second", slug = "taken" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Valid name", "ab")]
    [InlineData("Valid name", "Upper-Case")]
    [InlineData("Valid name", "double--hyphen")]
    [InlineData("Valid name", "-leading")]
    [InlineData("Valid name", "trailing-")]
    [InlineData("Valid name", "with space")]
    [InlineData("Valid name", "a123456789a123456789a123456789a123456789a123456789x")]
    [InlineData("Valid name", "")]
    public async Task An_invalid_slug_returns_400_with_a_slug_error(string name, string slug)
    {
        var response = await PlatformAdminClient().PostAsJsonAsync("/api/platform/tenants", new { name, slug });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("slug", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_name_returns_400_with_a_name_error(string name)
    {
        var response = await PlatformAdminClient().PostAsJsonAsync("/api/platform/tenants", new { name, slug = "valid-slug" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task The_list_is_paged_ordered_by_name_and_capped_at_50()
    {
        var client = PlatformAdminClient();
        for (var i = 1; i <= 55; i++)
        {
            var created = await client.PostAsJsonAsync("/api/platform/tenants", new { name = $"Tenant {i:D2}", slug = $"tenant-{i}" });
            created.EnsureSuccessStatusCode();
        }

        var secondPage = await client.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?page=2&pageSize=10");
        Assert.Equal(55, secondPage!.TotalCount);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(10, secondPage.PageSize);
        Assert.Equal(Enumerable.Range(11, 10).Select(i => $"Tenant {i:D2}"), secondPage.Items.Select(t => t.Name));

        var defaultPage = await client.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants");
        Assert.Equal(20, defaultPage!.PageSize);
        Assert.Equal(20, defaultPage.Items.Count);

        var capped = await client.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=500");
        Assert.Equal(50, capped!.PageSize);
        Assert.Equal(50, capped.Items.Count);
    }

    [Fact]
    public async Task Non_Platform_Admins_get_403_on_create_and_list()
    {
        var client = OutsiderClient();

        var create = await client.PostAsJsonAsync("/api/platform/tenants", new { name = "Nope", slug = "nope" });
        var list = await client.GetAsync("/api/platform/tenants");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }
}
