using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminTenantTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task An_Admin_gets_their_own_Tenant()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateTenantWithAdmin("tenant-b");

        var tenant = await a.Client.GetFromJsonAsync<CurrentTenantBody>("/api/admin/tenant");

        Assert.Equal(new CurrentTenantBody(a.Tenant.Id, a.Tenant.Name, a.Tenant.Slug), tenant);
    }

    [Fact]
    public async Task A_user_without_membership_gets_403()
    {
        var response = await OutsiderClient().GetAsync("/api/admin/tenant");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_Platform_Admin_without_membership_gets_403()
    {
        var response = await PlatformAdminClient().GetAsync("/api/admin/tenant");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_routes_return_401_without_a_token()
    {
        var response = await Client.GetAsync("/api/admin/tenant");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_Admin_of_a_Deactivated_tenant_gets_403_Tenant_deactivated()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await PlatformAdminClient().PostAsync($"/api/platform/tenants/{a.Tenant.Id}/deactivate", null);

        var response = await a.Client.GetAsync("/api/admin/tenant");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Tenant deactivated", problem!.Title);
        Assert.Equal(403, problem.Status);
    }

    [Fact]
    public async Task Me_returns_the_Admins_tenantId_and_null_for_others()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var adminMe = await a.Client.GetFromJsonAsync<MeBody>("/api/me");
        var outsiderMe = await OutsiderClient().GetFromJsonAsync<MeBody>("/api/me");

        Assert.Equal(a.Tenant.Id, adminMe!.TenantId);
        Assert.Null(outsiderMe!.TenantId);
    }

    private sealed record MeBody(Guid UserId, string? Email, bool IsPlatformAdmin, Guid? TenantId);
}
