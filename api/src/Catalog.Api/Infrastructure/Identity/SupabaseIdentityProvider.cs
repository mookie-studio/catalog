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
        using var request = CreateRequest(HttpMethod.Post, "auth/v1/invite");
        var redirectTo = options.Value.InviteRedirectUrl;
        request.Content = JsonContent.Create(string.IsNullOrWhiteSpace(redirectTo)
            ? new InviteBody(email, null)
            : new InviteBody(email, redirectTo));

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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ServiceRoleKey);
        return request;
    }

    private sealed record InviteBody(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("redirect_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RedirectTo);

    private sealed record UserBody([property: JsonPropertyName("id")] Guid Id);
}
