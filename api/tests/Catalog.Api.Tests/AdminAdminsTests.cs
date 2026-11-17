using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminAdminsTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task An_Admin_can_invite_a_colleague_who_then_appears_in_the_list()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "colleague@tenant-a.test" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("colleague@tenant-a.test", Factory.IdentityProvider.Invitations);
        var admins = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins");
        Assert.Equal(["admin@tenant-a.test", "colleague@tenant-a.test"], admins!.Items.Select(x => x.Email));
    }

    [Fact]
    public async Task The_colleague_can_then_use_the_admin_area_of_the_same_Tenant()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var colleague = await (await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "colleague@tenant-a.test" }))
            .Content.ReadFromJsonAsync<AdminBody>();

        var tenant = await ClientFor(colleague!.UserId, colleague.Email).GetFromJsonAsync<CurrentTenantBody>("/api/admin/tenant");

        Assert.Equal(a.Tenant.Id, tenant!.Id);
    }

    [Fact]
    public async Task Inviting_an_invalid_email_returns_400_and_an_existing_Admin_returns_409()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateTenantWithAdmin("tenant-b");

        Assert.Equal(HttpStatusCode.BadRequest, (await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "nope" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "admin@tenant-a.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "admin@tenant-b.test" })).StatusCode);
    }

    [Fact]
    public async Task An_Admin_can_remove_a_colleague()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var colleague = await (await a.Client.PostAsJsonAsync("/api/admin/admins", new { email = "colleague@tenant-a.test" }))
            .Content.ReadFromJsonAsync<AdminBody>();

        var response = await a.Client.DeleteAsync($"/api/admin/admins/{colleague!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(colleague.UserId, Factory.IdentityProvider.Deletions);
        var admins = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins");
        Assert.DoesNotContain(admins!.Items, x => x.Id == colleague.Id);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await ClientFor(colleague.UserId, colleague.Email).GetAsync("/api/admin/tenant")).StatusCode);
    }

    [Fact]
    public async Task An_Admin_cannot_remove_themselves()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.DeleteAsync($"/api/admin/admins/{a.Admin.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Factory.IdentityProvider.Deletions);
    }

    [Fact]
    public async Task Isolation_an_Admin_of_A_never_sees_or_removes_Admins_of_B()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");

        var admins = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins");
        var removal = await a.Client.DeleteAsync($"/api/admin/admins/{b.Admin.Id}");

        Assert.DoesNotContain(admins!.Items, x => x.Id == b.Admin.Id);
        Assert.Equal(HttpStatusCode.NotFound, removal.StatusCode);
        Assert.Contains((await b.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins"))!.Items, x => x.Id == b.Admin.Id);
    }
}
