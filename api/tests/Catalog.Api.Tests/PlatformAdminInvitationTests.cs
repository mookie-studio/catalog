using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class PlatformAdminInvitationTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Inviting_an_Admin_records_the_invitation_and_lists_the_Admin()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "acme");

        var response = await client.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email = "Owner@Acme.test " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var admin = await response.Content.ReadFromJsonAsync<AdminBody>();
        Assert.Equal("owner@acme.test", admin!.Email);
        Assert.Equal(Factory.IdentityProvider.UserIdFor("owner@acme.test"), admin.UserId);
        Assert.Contains("owner@acme.test", Factory.IdentityProvider.Invitations);

        var admins = await client.GetFromJsonAsync<List<AdminBody>>($"/api/platform/tenants/{tenant.Id}/admins");
        Assert.Equal(admin, Assert.Single(admins!));
    }

    [Fact]
    public async Task Inviting_the_same_email_twice_returns_409()
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "acme");
        await client.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email = "owner@acme.test" });

        var response = await client.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email = "owner@acme.test" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_Admin_of_another_Tenant_returns_409()
    {
        var client = PlatformAdminClient();
        var first = await CreateTenant(client, "first");
        var second = await CreateTenant(client, "second");
        await client.PostAsJsonAsync($"/api/platform/tenants/{first.Id}/admins", new { email = "owner@acme.test" });

        var response = await client.PostAsJsonAsync($"/api/platform/tenants/{second.Id}/admins", new { email = "owner@acme.test" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<AdminBody>>($"/api/platform/tenants/{second.Id}/admins"))!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    public async Task An_invalid_email_returns_400(string email)
    {
        var client = PlatformAdminClient();
        var tenant = await CreateTenant(client, "acme");

        var response = await client.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("email", problem!.Errors.Keys);
        Assert.Empty(Factory.IdentityProvider.Invitations);
    }

    [Fact]
    public async Task An_unknown_Tenant_returns_404()
    {
        var client = PlatformAdminClient();
        var id = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/platform/tenants/{id}/admins", new { email = "a@b.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/platform/tenants/{id}/admins")).StatusCode);
    }

    [Fact]
    public async Task Non_Platform_Admins_get_403()
    {
        var tenant = await CreateTenant(PlatformAdminClient(), "acme");
        var client = OutsiderClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email = "a@b.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/platform/tenants/{tenant.Id}/admins")).StatusCode);
    }
}
