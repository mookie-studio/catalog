using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class AdminTenantLogoTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Uploading_a_Logo_shows_it_in_the_profile_and_the_public_tenant()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PutAsync("/api/admin/tenant/logo", TestImages.Form(TestImages.Png(), "logo.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logo = await response.Content.ReadFromJsonAsync<LogoBody>();
        Assert.StartsWith($"https://images.test/{a.Tenant.Id}/logo/", logo!.LogoUrl);
        Assert.EndsWith(".png", logo.LogoUrl);
        Assert.Equal(TestImages.Png(), Factory.ImageStorage.Files[InMemoryImageStorage.KeyOf(logo.LogoUrl)].Content);
        Assert.Equal(logo.LogoUrl, (await a.Client.GetFromJsonAsync<LogoBody>("/api/admin/tenant/profile"))!.LogoUrl);
        Assert.Equal(logo.LogoUrl, (await Client.GetFromJsonAsync<LogoBody>("/api/public/tenant-a"))!.LogoUrl);
    }

    [Fact]
    public async Task Replacing_a_Logo_removes_the_old_object()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var first = await Upload(a.Client, TestImages.Png());

        var second = await Upload(a.Client, TestImages.WebP());

        Assert.NotEqual(first, second);
        Assert.Equal([InMemoryImageStorage.KeyOf(second)], Factory.ImageStorage.Files.Keys);
        Assert.Equal(second, (await a.Client.GetFromJsonAsync<LogoBody>("/api/admin/tenant/profile"))!.LogoUrl);
    }

    [Fact]
    public async Task Removing_the_Logo_clears_it()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await Upload(a.Client, TestImages.Jpeg());

        var response = await a.Client.DeleteAsync("/api/admin/tenant/logo");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Factory.ImageStorage.Files);
        Assert.Null((await a.Client.GetFromJsonAsync<LogoBody>("/api/admin/tenant/profile"))!.LogoUrl);
        Assert.Null((await Client.GetFromJsonAsync<LogoBody>("/api/public/tenant-a"))!.LogoUrl);
    }

    [Fact]
    public async Task Removing_when_there_is_no_Logo_is_204()
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        Assert.Equal(HttpStatusCode.NoContent, (await a.Client.DeleteAsync("/api/admin/tenant/logo")).StatusCode);
    }

    public static TheoryData<string, byte[]> InvalidFiles => new()
    {
        { "gif", TestImages.Gif() },
        { "empty", [] },
        { "too big", TestImages.Png(1024 * 1024 + 1) },
        { "svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray() },
    };

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public async Task Invalid_files_return_400_on_file(string name, byte[] content)
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PutAsync("/api/admin/tenant/logo", TestImages.Form(content, name));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["file"], problem!.Errors.Keys);
        Assert.Empty(Factory.ImageStorage.Files);
    }

    [Fact]
    public async Task Isolation_tenant_As_Logo_never_shows_on_tenant_B()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await Upload(a.Client, TestImages.Png());

        Assert.Null((await b.Client.GetFromJsonAsync<LogoBody>("/api/admin/tenant/profile"))!.LogoUrl);
        Assert.Null((await Client.GetFromJsonAsync<LogoBody>("/api/public/tenant-b"))!.LogoUrl);

        await b.Client.DeleteAsync("/api/admin/tenant/logo");
        Assert.Single(Factory.ImageStorage.Files);
    }

    private static async Task<string> Upload(HttpClient client, byte[] content)
    {
        var response = await client.PutAsync("/api/admin/tenant/logo", TestImages.Form(content, "logo"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LogoBody>())!.LogoUrl!;
    }

    private sealed record LogoBody(string? LogoUrl);
}
