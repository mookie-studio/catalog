using System.Net;
using System.Text;
using System.Text.Json;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Tests;

/// <summary>
/// Verifies the HTTP requests sent to Supabase. The identity provider is one of the two
/// fakes in the HTTP tests, so its real implementation is checked here at its HTTP boundary.
/// </summary>
public sealed class SupabaseIdentityProviderTests
{
    private const string ServiceRoleKey = "service-role-key";

    [Fact]
    public async Task Invite_posts_the_email_and_redirect_with_service_role_headers()
    {
        var userId = Guid.CreateVersion7();
        var handler = new StubHandler(HttpStatusCode.OK, $$"""{"id":"{{userId}}","email":"new@catalog.test"}""");
        var provider = CreateProvider(handler, inviteRedirectUrl: "https://app.catalog.test/accept");

        var result = await provider.InviteUserAsync("new@catalog.test", CancellationToken.None);

        Assert.Equal(userId, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        // Supabase Auth only honours redirect_to as a query parameter.
        Assert.Equal(
            "https://project.supabase.test/auth/v1/invite?redirect_to=https%3A%2F%2Fapp.catalog.test%2Faccept", request.Uri);
        Assert.Equal(ServiceRoleKey, request.ApiKey);
        Assert.Equal($"Bearer {ServiceRoleKey}", request.Authorization);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("new@catalog.test", body.RootElement.GetProperty("email").GetString());
        Assert.False(body.RootElement.TryGetProperty("redirect_to", out _));
    }

    [Fact]
    public async Task Invite_omits_redirect_when_not_configured()
    {
        var handler = new StubHandler(HttpStatusCode.OK, $$"""{"id":"{{Guid.CreateVersion7()}}"}""");
        var provider = CreateProvider(handler, inviteRedirectUrl: null);

        await provider.InviteUserAsync("new@catalog.test", CancellationToken.None);

        Assert.Equal("https://project.supabase.test/auth/v1/invite", Assert.Single(handler.Requests).Uri);
    }

    [Fact]
    public async Task Delete_sends_a_delete_to_the_admin_users_endpoint()
    {
        var userId = Guid.CreateVersion7();
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler, inviteRedirectUrl: null);

        await provider.DeleteUserAsync(userId, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"https://project.supabase.test/auth/v1/admin/users/{userId}", request.Uri);
        Assert.Equal(ServiceRoleKey, request.ApiKey);
        Assert.Equal($"Bearer {ServiceRoleKey}", request.Authorization);
    }

    [Fact]
    public void Options_never_print_the_service_role_key()
    {
        var options = new SupabaseOptions { Url = "https://project.supabase.test", ServiceRoleKey = ServiceRoleKey };

        Assert.DoesNotContain(ServiceRoleKey, options.ToString());
    }

    private static SupabaseIdentityProvider CreateProvider(StubHandler handler, string? inviteRedirectUrl) =>
        new(new HttpClient(handler), Options.Create(new SupabaseOptions
        {
            Url = "https://project.supabase.test/",
            ServiceRoleKey = ServiceRoleKey,
            InviteRedirectUrl = inviteRedirectUrl,
        }));

    private sealed record CapturedRequest(HttpMethod Method, string Uri, string? ApiKey, string? Authorization, string? Body);

    private sealed class StubHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!.ToString(),
                request.Headers.TryGetValues("apikey", out var keys) ? keys.Single() : null,
                request.Headers.Authorization?.ToString(),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
