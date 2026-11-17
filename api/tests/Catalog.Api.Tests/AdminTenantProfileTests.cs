using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminTenantProfileTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static readonly object FullProfile = new
    {
        description = "  Tools for every job.  ",
        contactEmail = "hello@acme.example",
        phone = "+34 600 000 000",
        address = "Main Street 1, Madrid",
        websiteUrl = "https://acme.example",
        brandColor = "#1a2b3c",
    };

    [Fact]
    public async Task A_new_tenant_has_an_empty_profile()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var profile = await a.Client.GetFromJsonAsync<TenantProfileBody>("/api/admin/tenant/profile");

        Assert.Equal(new TenantProfileBody(a.Tenant.Name, "tenant-a", null, null, null, null, null, null), profile);
    }

    [Fact]
    public async Task An_Admin_saves_and_reads_back_the_profile()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var put = await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", FullProfile);
        put.EnsureSuccessStatusCode();
        var profile = await a.Client.GetFromJsonAsync<TenantProfileBody>("/api/admin/tenant/profile");

        Assert.Equal(
            new TenantProfileBody(
                a.Tenant.Name, "tenant-a", "Tools for every job.", "hello@acme.example", "+34 600 000 000",
                "Main Street 1, Madrid", "https://acme.example", "#1A2B3C"),
            profile);
    }

    [Fact]
    public async Task Empty_strings_are_stored_as_null()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", FullProfile);

        var put = await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new
        {
            description = "", contactEmail = " ", phone = "", address = "", websiteUrl = "", brandColor = "",
        });
        put.EnsureSuccessStatusCode();
        var profile = await a.Client.GetFromJsonAsync<TenantProfileBody>("/api/admin/tenant/profile");

        Assert.Equal(new TenantProfileBody(a.Tenant.Name, "tenant-a", null, null, null, null, null, null), profile);
    }

    [Theory]
    [InlineData("contactEmail", "not-an-email")]
    [InlineData("websiteUrl", "acme.example")]
    [InlineData("websiteUrl", "ftp://acme.example")]
    [InlineData("brandColor", "red")]
    [InlineData("brandColor", "#12345")]
    public async Task Invalid_values_return_400_on_their_field(string field, string value)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var body = new Dictionary<string, string?> { [field] = value };

        var response = await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal([field], problem!.Errors.Keys);
    }

    [Theory]
    [InlineData("description", 2001)]
    [InlineData("phone", 41)]
    [InlineData("address", 301)]
    public async Task Values_over_their_max_length_return_400(string field, int length)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var body = new Dictionary<string, string?> { [field] = new string('x', length) };

        var response = await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal([field], problem!.Errors.Keys);
    }

    [Fact]
    public async Task The_public_tenant_shows_the_saved_profile()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", FullProfile);

        var tenant = await Client.GetFromJsonAsync<PublicTenantBody>("/api/public/tenant-a");

        Assert.Equal(
            new PublicTenantBody(
                a.Tenant.Name, "tenant-a", "Tools for every job.", "hello@acme.example", "+34 600 000 000",
                "Main Street 1, Madrid", "https://acme.example", "#1A2B3C"),
            tenant);
    }

    [Fact]
    public async Task Isolation_tenant_As_profile_never_appears_for_tenant_B()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", FullProfile);

        var adminB = await b.Client.GetFromJsonAsync<TenantProfileBody>("/api/admin/tenant/profile");
        var publicB = await Client.GetFromJsonAsync<PublicTenantBody>("/api/public/tenant-b");

        Assert.Equal(new TenantProfileBody(b.Tenant.Name, "tenant-b", null, null, null, null, null, null), adminB);
        Assert.Null(publicB!.Description);
        Assert.Null(publicB.BrandColor);
    }

    [Fact]
    public async Task Unauthenticated_calls_get_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/admin/tenant/profile")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Client.PutAsJsonAsync("/api/admin/tenant/profile", FullProfile)).StatusCode);
    }

    private sealed record TenantProfileBody(
        string Name, string Slug, string? Description, string? ContactEmail, string? Phone, string? Address,
        string? WebsiteUrl, string? BrandColor, string? LogoUrl = null);

    private sealed record PublicTenantBody(
        string Name, string Slug, string? Description, string? ContactEmail, string? Phone, string? Address,
        string? WebsiteUrl, string? BrandColor, string? LogoUrl = null);
}
