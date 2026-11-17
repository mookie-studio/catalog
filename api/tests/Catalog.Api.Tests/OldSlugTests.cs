using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class OldProductSlugTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Changing_a_slug_twice_keeps_both_previous_values_reserved()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreateProduct(a.Client, "Drill", "D-1", "x");
        var saw = await CreateProduct(a.Client, "Saw", "S-1", "x");
        await ChangeSlug(a.Client, drill, "drill-2000");
        await ChangeSlug(a.Client, drill, "drill-3000");

        var first = await ChangeSlug(a.Client, saw, "drill");
        var second = await ChangeSlug(a.Client, saw, "drill-2000");

        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(["slug"], (await second.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task Generated_slugs_skip_Old_slugs()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreateProduct(a.Client, "Drill", "D-1", "x");
        await ChangeSlug(a.Client, drill, "power-drill");

        var another = await CreateProduct(a.Client, "Drill", "D-2");

        Assert.Equal("drill-2", another.Slug);
    }

    [Fact]
    public async Task A_product_can_go_back_to_its_own_Old_slug()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreateProduct(a.Client, "Drill", "D-1", "x");
        await ChangeSlug(a.Client, drill, "power-drill");

        var back = await ChangeSlug(a.Client, drill, "drill");
        var forward = await ChangeSlug(a.Client, drill, "power-drill");

        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal(HttpStatusCode.OK, forward.StatusCode);
        Assert.Equal("power-drill", (await forward.Content.ReadFromJsonAsync<ProductBody>())!.Slug);
    }

    [Fact]
    public async Task Old_slugs_of_another_Tenant_never_block()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var theirs = await CreateProduct(b.Client, "Drill", "D-1", "x");
        await ChangeSlug(b.Client, theirs, "power-drill");

        var mine = await CreateProduct(a.Client, "Drill", "D-1", "x");
        var other = await CreateProduct(a.Client, "Saw", "S-1", "x");

        Assert.Equal("drill", mine.Slug);
        Assert.Equal(HttpStatusCode.OK, (await ChangeSlug(a.Client, other, "drill-old")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChangeSlug(a.Client, mine, "power")).StatusCode);
    }

    [Fact]
    public async Task An_Old_slug_returns_the_Published_product_with_its_current_slug()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreatePublishedProduct(a.Client, "Drill", "D-1", "x");
        await ChangeSlug(a.Client, drill, "power-drill");

        var response = await Client.GetAsync("/api/public/tenant-a/products/drill");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<PublicProductBody>();
        Assert.Equal("power-drill", detail!.Slug);
        Assert.Equal("Drill", detail.Name);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Archived")]
    public async Task An_Old_slug_of_an_unpublished_product_returns_404(string status)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreatePublishedProduct(a.Client, "Drill", "D-1", "x");
        await ChangeSlug(a.Client, drill, "power-drill");
        await a.Client.PostAsJsonAsync($"/api/admin/products/{drill.Id}/status", new { status });

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/drill")).StatusCode);
    }

    [Fact]
    public async Task An_Old_slug_of_another_Tenant_returns_404()
    {
        await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var theirs = await CreatePublishedProduct(b.Client, "Drill", "D-1", "x");
        await ChangeSlug(b.Client, theirs, "power-drill");

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/tenant-a/products/drill")).StatusCode);
    }

    private static Task<HttpResponseMessage> ChangeSlug(HttpClient client, ProductBody product, string slug) =>
        client.PutAsJsonAsync($"/api/admin/products/{product.Id}", new
        {
            name = product.Name,
            sku = product.Sku,
            description = product.Description ?? "x",
            slug,
        });
}

public sealed class OldTenantSlugTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task After_a_change_another_Tenant_cannot_use_the_old_slug()
    {
        var platform = PlatformAdminClient();
        var tenant = await CreateTenant(platform, "acme");
        var other = await CreateTenant(platform, "other");
        await ChangeSlug(platform, tenant.Id, "acme-tools");

        var create = await platform.PostAsJsonAsync("/api/platform/tenants", new { name = "Acme", slug = "acme" });
        var change = await ChangeSlug(platform, other.Id, "acme");

        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);
        Assert.Equal(["slug"], (await create.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal(["slug"], (await change.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task The_same_Tenant_can_go_back_to_its_old_slug()
    {
        var platform = PlatformAdminClient();
        var tenant = await CreateTenant(platform, "acme");
        await ChangeSlug(platform, tenant.Id, "acme-tools");

        var back = await ChangeSlug(platform, tenant.Id, "acme");
        var forward = await ChangeSlug(platform, tenant.Id, "acme-tools");

        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal(HttpStatusCode.OK, forward.StatusCode);
    }

    [Fact]
    public async Task Every_public_endpoint_works_through_an_old_Tenant_slug()
    {
        var a = await CreateTenantWithAdmin("acme");
        await CreateCategory(a.Client, "Tools");
        await CreateDefinition(a.Client, "Color", "OptionList", isFilterable: true, options: ["Red"]);
        await CreatePublishedProduct(a.Client, "Drill", "D-1", "x");
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true });
        await ChangeSlug(PlatformAdminClient(), a.Tenant.Id, "acme-tools");

        var tenant = await Client.GetFromJsonAsync<CurrentTenantBody>("/api/public/acme");
        var products = await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>("/api/public/acme/products");
        var detail = await Client.GetAsync("/api/public/acme/products/drill");
        var categories = await Client.GetAsync("/api/public/acme/categories");
        var filters = await Client.GetAsync("/api/public/acme/filters");
        var inquiry = await Client.PostAsJsonAsync("/api/public/acme/products/drill/inquiries",
            new { name = "Ana", email = "ana@example.test", message = "Is it in stock?" });

        Assert.Equal("acme-tools", tenant!.Slug);
        Assert.Equal("drill", Assert.Single(products!.Items).Slug);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Equal(HttpStatusCode.OK, filters.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, inquiry.StatusCode);
    }

    [Fact]
    public async Task An_old_slug_of_a_Deactivated_Tenant_returns_404()
    {
        var platform = PlatformAdminClient();
        var tenant = await CreateTenant(platform, "acme");
        await ChangeSlug(platform, tenant.Id, "acme-tools");
        await platform.PostAsync($"/api/platform/tenants/{tenant.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/acme")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/public/acme-tools")).StatusCode);
    }

    [Fact]
    public async Task Only_Platform_Admins_can_change_a_Tenant_slug()
    {
        var a = await CreateTenantWithAdmin("acme");

        var response = await ChangeSlug(a.Client, a.Tenant.Id, "acme-tools");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/public/acme")).StatusCode);
    }

    private static async Task<HttpResponseMessage> ChangeSlug(HttpClient client, Guid tenantId, string slug) =>
        await client.PutAsJsonAsync($"/api/platform/tenants/{tenantId}/slug", new { slug });
}
