using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PlatformTenantLifecycleTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Deactivating_and_reactivating_is_reflected_in_the_list_and_idempotent()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "acme");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/deactivate", null)).StatusCode);
        Assert.False((await Find(client, tenant.Id)).IsActive);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/reactivate", null)).StatusCode);
        Assert.True((await Find(client, tenant.Id)).IsActive);
    }

    [Fact]
    public async Task Changing_the_slug_is_reflected_in_the_list()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "old-slug");

        var response = await client.PutAsJsonAsync($"/api/platform/tenants/{tenant.Id}/slug", new { slug = "new-slug" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("new-slug", (await Find(client, tenant.Id)).Slug);
    }

    [Fact]
    public async Task Changing_to_a_slug_in_use_returns_409()
    {
        var client = PlatformAdminClient();
        await CreateTenant(client, "taken");
        var tenant = await CreateTenant(client, "mine");

        var response = await client.PutAsJsonAsync($"/api/platform/tenants/{tenant.Id}/slug", new { slug = "taken" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Changing_to_an_invalid_slug_returns_400()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "mine");

        var response = await client.PutAsJsonAsync($"/api/platform/tenants/{tenant.Id}/slug", new { slug = "Not Valid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_ids_return_404()
    {
        var client = PlatformAdminClient();
        var id = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/platform/tenants/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/platform/tenants/{id}/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/platform/tenants/{id}/slug", new { slug = "whatever" })).StatusCode);
    }

    [Fact]
    public async Task There_is_no_delete_endpoint()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "keep");

        var response = await client.DeleteAsync($"/api/platform/tenants/{tenant.Id}");

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(await Find(client, tenant.Id), new[] { tenant });
    }

    [Fact]
    public async Task Non_Platform_Admins_get_403()
    {
        var tenant = await CreateTenant(PlatformAdminClient(), "acme");
        var client = OutsiderClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/platform/tenants/{tenant.Id}/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/platform/tenants/{tenant.Id}/slug", new { slug = "x-y-z" })).StatusCode);
    }

    private static async Task<TenantBody> Find(HttpClient client, Guid id)
    {
        var list = await client.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=50");
        return list!.Items.Single(t => t.Id == id);
    }
}
