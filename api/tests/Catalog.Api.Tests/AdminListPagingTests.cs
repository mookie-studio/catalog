using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminListPagingTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Twenty_five_Admins_give_two_pages_ordered_by_email()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await InviteColleagues(a.Client, 24);

        var first = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins");
        var second = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins?page=2");

        Assert.Equal((1, 20, 25), (first!.Page, first.PageSize, first.TotalCount));
        Assert.Equal(20, first.Items.Count);
        Assert.Equal((2, 20, 25), (second!.Page, second.PageSize, second.TotalCount));
        Assert.Equal(5, second.Items.Count);
        var emails = first.Items.Concat(second.Items).Select(x => x.Email).ToList();
        Assert.Equal(emails.Order(StringComparer.Ordinal), emails);
        Assert.Equal(25, emails.Distinct().Count());
    }

    [Fact]
    public async Task The_platform_Admins_list_is_paged_and_pageSize_is_capped_at_50()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await InviteColleagues(a.Client, 24);
        var platform = PlatformAdminClient();

        var page = await platform.GetFromJsonAsync<PagedBody<AdminBody>>($"/api/platform/tenants/{a.Tenant.Id}/admins?page=2&pageSize=10");
        var capped = await platform.GetFromJsonAsync<PagedBody<AdminBody>>($"/api/platform/tenants/{a.Tenant.Id}/admins?pageSize=500");
        var cappedAdmin = await a.Client.GetFromJsonAsync<PagedBody<AdminBody>>("/api/admin/admins?pageSize=500");

        Assert.Equal((2, 10, 25), (page!.Page, page.PageSize, page.TotalCount));
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(50, capped!.PageSize);
        Assert.Equal(25, capped.Items.Count);
        Assert.Equal(50, cappedAdmin!.PageSize);
    }

    private static async Task InviteColleagues(HttpClient client, int count)
    {
        for (var i = 0; i < count; i++)
        {
            (await client.PostAsJsonAsync("/api/admin/admins", new { email = $"colleague{i:00}@tenant-a.test" })).EnsureSuccessStatusCode();
        }
    }
}
