using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class InquiryTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static readonly object ValidInquiry = new { name = "Ana", email = "ana@example.com", message = "Is it in stock in red?" };

    private async Task<AdminSession> TenantWithInquiries(string slug = "tenant-a", bool enabled = true)
    {
        var admin = await CreateTenantWithAdmin(slug);
        (await admin.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = enabled })).EnsureSuccessStatusCode();
        return admin;
    }

    [Fact]
    public async Task Inquiries_are_disabled_by_default_and_the_toggle_persists()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        Assert.False((await Client.GetFromJsonAsync<InquiriesFlag>("/api/public/tenant-a"))!.InquiriesEnabled);

        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true });
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { description = "Tools" });

        Assert.True((await a.Client.GetFromJsonAsync<InquiriesFlag>("/api/admin/tenant/profile"))!.InquiriesEnabled);
        Assert.True((await Client.GetFromJsonAsync<InquiriesFlag>("/api/public/tenant-a"))!.InquiriesEnabled);
    }

    [Fact]
    public async Task A_Visitor_sends_an_Inquiry_and_the_Admin_sees_it_with_its_product()
    {
        var a = await TenantWithInquiries();
        var product = await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");

        var response = await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var list = await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries");
        var inquiry = Assert.Single(list!.Items);
        Assert.Equal(("Ana", "ana@example.com", "Is it in stock in red?", "Open"), (inquiry.Name, inquiry.Email, inquiry.Message, inquiry.Status));
        Assert.Equal(new InquiryProductBody(product.Id, "Hammer drill", "hammer-drill"), inquiry.Product);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Archived")]
    public async Task Products_that_are_not_Published_return_404(string status)
    {
        var a = await TenantWithInquiries();
        var product = await CreateProduct(a.Client, "Hammer drill", "HD-1", "Description");
        if (status != "Draft")
        {
            await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status });
        }

        var response = await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_Inquiries_return_404()
    {
        var a = await TenantWithInquiries(enabled: false);
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");

        var response = await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_and_Deactivated_tenants_return_404()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        await PlatformAdminClient().PostAsync($"/api/platform/tenants/{a.Tenant.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound,
            (await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Client.PostAsJsonAsync("/api/public/nobody/products/hammer-drill/inquiries", ValidInquiry)).StatusCode);
    }

    [Fact]
    public async Task Missing_and_invalid_fields_return_400_per_field()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");

        var response = await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries",
            new { name = " ", email = "not-an-email", message = new string('x', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["email", "message", "name"], problem!.Errors.Keys.Order());
        Assert.Empty((await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items);
    }

    [Fact]
    public async Task A_filled_honeypot_returns_202_and_saves_nothing()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");

        var response = await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries",
            new { name = "Bot", email = "bot@example.com", message = "Buy now", website = "https://spam.example" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty((await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items);
    }

    [Fact]
    public async Task The_list_is_newest_first_paged_and_filtered_by_status()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        foreach (var name in new[] { "First", "Second", "Third" })
        {
            await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries",
                new { name, email = "v@example.com", message = "Hello" });
        }

        var all = await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries?pageSize=2");
        Assert.Equal(["Third", "Second"], all!.Items.Select(i => i.Name));
        Assert.Equal(3, all.TotalCount);
        var second = await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries?pageSize=2&page=2");
        Assert.Equal(["First"], second!.Items.Select(i => i.Name));

        await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{all.Items[0].Id}/status", new { status = "Handled" });

        var open = await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries?status=Open");
        var handled = await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries?status=Handled");
        Assert.Equal(["Second", "First"], open!.Items.Select(i => i.Name));
        Assert.Equal(["Third"], handled!.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Status_goes_Open_to_Handled_and_back()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry);
        var id = (await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items[0].Id;

        var handled = await (await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{id}/status", new { status = "Handled" }))
            .Content.ReadFromJsonAsync<InquiryBody>();
        var reopened = await (await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{id}/status", new { status = "Open" }))
            .Content.ReadFromJsonAsync<InquiryBody>();

        Assert.Equal("Handled", handled!.Status);
        Assert.Equal("Open", reopened!.Status);
        var invalid = await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{id}/status", new { status = "Closed" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task A_deleted_Inquiry_no_longer_appears()
    {
        var a = await TenantWithInquiries();
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        await Client.PostAsJsonAsync("/api/public/tenant-a/products/hammer-drill/inquiries", ValidInquiry);
        var id = (await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items[0].Id;

        Assert.Equal(HttpStatusCode.NoContent, (await a.Client.DeleteAsync($"/api/admin/inquiries/{id}")).StatusCode);

        Assert.Empty((await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.DeleteAsync($"/api/admin/inquiries/{id}")).StatusCode);
    }

    [Fact]
    public async Task Isolation_an_Admin_of_tenant_A_never_reaches_tenant_Bs_Inquiries()
    {
        var a = await TenantWithInquiries("tenant-a");
        var b = await TenantWithInquiries("tenant-b");
        await CreatePublishedProduct(b.Client, "Hammer drill", "HD-1");
        await Client.PostAsJsonAsync("/api/public/tenant-b/products/hammer-drill/inquiries", ValidInquiry);
        var id = (await b.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items[0].Id;

        Assert.Empty((await a.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound,
            (await a.Client.PutAsJsonAsync($"/api/admin/inquiries/{id}/status", new { status = "Handled" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.DeleteAsync($"/api/admin/inquiries/{id}")).StatusCode);
        Assert.Single((await b.Client.GetFromJsonAsync<PagedBody<InquiryBody>>("/api/admin/inquiries"))!.Items);
    }

    private sealed record InquiriesFlag(bool InquiriesEnabled);
}

public sealed record InquiryProductBody(Guid Id, string Name, string Slug);

public sealed record InquiryBody(Guid Id, string Name, string Email, string Message, string Status, DateTimeOffset CreatedAt, InquiryProductBody Product);
