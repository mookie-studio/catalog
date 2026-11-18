using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminDashboardTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task A_new_Tenant_has_all_zeros_and_no_Categories()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        Assert.Equal(new DashboardBody(0, 0, 0, 0, 0, 0, 0, false), await Get(a.Client));
    }

    [Fact]
    public async Task Every_count_follows_the_catalog()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        (await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true })).EnsureSuccessStatusCode();
        var category = await CreateCategory(a.Client, "Tools");
        await CreateProduct(a.Client, "Draft one", "D-1");
        var hammer = await CreatePublishedProduct(a.Client, "Hammer", "H-1");
        await CreatePublishedProduct(a.Client, "Saw", "S-1");
        var old = await CreatePublishedProduct(a.Client, "Old", "O-1");
        (await a.Client.PostAsJsonAsync($"/api/admin/products/{old.Id}/status", new { status = "Archived" })).EnsureSuccessStatusCode();
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{hammer.Id}/featured", new { featured = true })).EnsureSuccessStatusCode();
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{hammer.Id}/categories", new { categoryIds = new[] { category.Id } })).EnsureSuccessStatusCode();
        (await a.Client.PostAsync($"/api/admin/products/{hammer.Id}/images", TestImages.Form(TestImages.Png(), "a.png"))).EnsureSuccessStatusCode();
        var inquiry = new { name = "Ana", email = "ana@example.com", message = "Hello there" };
        (await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer/inquiries", inquiry)).EnsureSuccessStatusCode();
        (await Client.PostAsJsonAsync("/api/public/tenant-a/products/saw/inquiries", inquiry)).EnsureSuccessStatusCode();
        var inbox = await a.Client.GetFromJsonAsync<PagedBody<InquiryIdBody>>("/api/admin/inquiries");
        (await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{inbox!.Items[0].Id}/status", new { status = "Handled" })).EnsureSuccessStatusCode();

        var dashboard = await Get(a.Client);

        Assert.Equal(new DashboardBody(1, 2, 1, 1, 1, 1, 1, true), dashboard);
    }

    [Fact]
    public async Task Isolation_data_of_B_never_changes_the_counts_of_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateCategory(b.Client, "Tools");
        await CreatePublishedProduct(b.Client, "Hammer", "H-1");

        Assert.Equal(new DashboardBody(0, 0, 0, 0, 0, 0, 0, false), await Get(a.Client));
    }

    private static async Task<DashboardBody> Get(HttpClient client) =>
        (await client.GetFromJsonAsync<DashboardBody>("/api/admin/dashboard"))!;

    private sealed record InquiryIdBody(Guid Id);
}
