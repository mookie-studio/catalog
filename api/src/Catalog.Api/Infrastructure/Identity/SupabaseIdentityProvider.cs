using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Infrastructure.Identity;

/// <summary>Invites and deletes users through the Supabase Auth admin API.</summary>
public sealed class SupabaseIdentityProvider(HttpClient http, IOptions<SupabaseOptions> options) : IIdentityProvider
{
    public async Task<Guid> InviteUserAsync(string email, CancellationToken ct)
    {
        // Supabase Auth reads redirect_to from the query string, not from the body.
        var redirectTo = options.Value.InviteRedirectUrl;
        var path = string.IsNullOrWhiteSpace(redirectTo)
            ? "auth/v1/invite"
            : $"auth/v1/invite?redirect_to={Uri.EscapeDataString(redirectTo)}";
        using var request = CreateRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(new InviteBody(email));

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserBody>(ct)
            ?? throw new InvalidOperationException("The identity provider returned an empty invitation response.");
        return user.Id;
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"auth/v1/admin/users/{userId}");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Url) || string.IsNullOrWhiteSpace(settings.ServiceRoleKey))
        {
            throw new InvalidOperationException("Supabase:Url and Supabase:ServiceRoleKey must be configured.");
        }

        var request = new HttpRequestMessage(method, new Uri(new Uri(settings.Url.TrimEnd('/') + "/"), path));
        request.Headers.Add("apikey", settings.ServiceRoleKey);

        // Secret keys (sb_secret_...) are not JWTs: Supabase rejects them on the Authorization header and
        // accepts them on apikey only. The legacy service_role key is a JWT and is sent on both.
        if (!settings.ServiceRoleKey.StartsWith("sb_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ServiceRoleKey);
        }

        return request;
    }

    private sealed record InviteBody([property: JsonPropertyName("email")] string Email);

    private sealed record UserBody([property: JsonPropertyName("id")] Guid Id);
}
