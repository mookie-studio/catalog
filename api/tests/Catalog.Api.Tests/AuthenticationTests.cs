using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AuthenticationTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Me_without_a_token_returns_401()
    {
        var response = await Client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_callers_user_id_and_email()
    {
        var userId = Guid.CreateVersion7();
        var client = ClientFor(userId, "someone@catalog.test");

        var me = await client.GetFromJsonAsync<MeBody>("/api/me");

        Assert.Equal(userId, me!.UserId);
        Assert.Equal("someone@catalog.test", me.Email);
        Assert.False(me.IsPlatformAdmin);
    }

    [Fact]
    public async Task Me_flags_a_configured_Platform_Admin()
    {
        var me = await PlatformAdminClient().GetFromJsonAsync<MeBody>("/api/me");

        Assert.Equal(TestTokens.PlatformAdminUserId, me!.UserId);
        Assert.True(me.IsPlatformAdmin);
    }

    [Fact]
    public async Task Platform_routes_return_403_for_non_Platform_Admins()
    {
        var response = await OutsiderClient().GetAsync("/api/platform/tenants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Platform_routes_return_401_without_a_token()
    {
        var response = await Client.GetAsync("/api/platform/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Platform_routes_are_reachable_for_a_Platform_Admin()
    {
        var response = await PlatformAdminClient().GetAsync("/api/platform/tenants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_returns_401()
    {
        var token = TestTokens.Create(Guid.CreateVersion7(), "late@catalog.test", expires: DateTime.UtcNow.AddMinutes(-5));

        var response = await SendMe(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_returns_401()
    {
        var token = TestTokens.Create(Guid.CreateVersion7(), "forger@catalog.test",
            secret: "another-secret-that-is-also-long-enough-for-hs256");

        var response = await SendMe(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendMe(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(request);
    }

    private sealed record MeBody(Guid UserId, string? Email, bool IsPlatformAdmin, Guid? TenantId);
}
