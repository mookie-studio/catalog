using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Catalog.DemoSeeder;

/// <summary>The demo accounts in the identity provider (ADR 0004): created with a password, and signed in.</summary>
public interface IDemoIdentity
{
    /// <summary>Creates the confirmed user with this password, or sets the password again when it exists. Returns its id.</summary>
    Task<Guid> EnsureUserAsync(string email, string password, CancellationToken ct);

    /// <summary>Signs in with email and password and returns an access token for the Catalog API.</summary>
    Task<string> SignInAsync(string email, string password, CancellationToken ct);
}

/// <summary>Demo accounts through the Supabase Auth admin API (service role key) and password sign-in (anon key).</summary>
public sealed class SupabaseDemoIdentity(HttpClient http, string supabaseUrl, string serviceRoleKey, string anonKey) : IDemoIdentity
{
    public async Task<Guid> EnsureUserAsync(string email, string password, CancellationToken ct)
    {
        using (var create = Admin(HttpMethod.Post, "auth/v1/admin/users"))
        {
            create.Content = JsonContent.Create(new UserBody(email, password, true));
            using var response = await http.SendAsync(create, ct);
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<UserResponse>(ct))!.Id;
            }

            var text = await response.Content.ReadAsStringAsync(ct);
            if (!IsAlreadyRegistered(response.StatusCode, text))
            {
                throw Failure("create the user", email, response.StatusCode, text);
            }
        }

        var id = await FindUserIdAsync(email, ct)
            ?? throw new InvalidOperationException($"Supabase says {email} exists, but it was not found in the user list.");

        using var update = Admin(HttpMethod.Put, $"auth/v1/admin/users/{id}");
        update.Content = JsonContent.Create(new UserBody(null, password, true));
        using var updated = await http.SendAsync(update, ct);
        if (!updated.IsSuccessStatusCode)
        {
            throw Failure("set the password of", email, updated.StatusCode, await updated.Content.ReadAsStringAsync(ct));
        }

        return id;
    }

    public async Task<string> SignInAsync(string email, string password, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url("auth/v1/token?grant_type=password"));
        request.Headers.Add("apikey", anonKey);
        request.Content = JsonContent.Create(new SignInBody(email, password));
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw Failure("sign in as", email, response.StatusCode, await response.Content.ReadAsStringAsync(ct));
        }

        return (await response.Content.ReadFromJsonAsync<TokenResponse>(ct))!.AccessToken;
    }

    private async Task<Guid?> FindUserIdAsync(string email, CancellationToken ct)
    {
        for (var page = 1; page <= 1000; page++)
        {
            using var request = Admin(HttpMethod.Get, $"auth/v1/admin/users?page={page}&per_page=100");
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw Failure("list users to find", email, response.StatusCode, await response.Content.ReadAsStringAsync(ct));
            }

            var users = (await response.Content.ReadFromJsonAsync<UserList>(ct))!.Users;
            if (users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)) is { } user)
            {
                return user.Id;
            }

            if (users.Count < 100)
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsAlreadyRegistered(HttpStatusCode status, string body) =>
        status is HttpStatusCode.UnprocessableEntity or HttpStatusCode.BadRequest or HttpStatusCode.Conflict
        && (body.Contains("email_exists", StringComparison.OrdinalIgnoreCase)
            || body.Contains("already been registered", StringComparison.OrdinalIgnoreCase)
            || body.Contains("already registered", StringComparison.OrdinalIgnoreCase));

    private HttpRequestMessage Admin(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, Url(path));
        request.Headers.Add("apikey", serviceRoleKey);

        // Secret keys (sb_secret_...) are not JWTs: Supabase only accepts them on apikey. The legacy
        // service_role key is a JWT and is sent on both headers (same rule as the API's SupabaseIdentityProvider).
        if (!serviceRoleKey.StartsWith("sb_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        }

        return request;
    }

    private Uri Url(string path) => new(new Uri(supabaseUrl.TrimEnd('/') + "/"), path);

    private static InvalidOperationException Failure(string action, string email, HttpStatusCode status, string body) =>
        new($"Supabase could not {action} {email}: {(int)status} {status} {(body.Length > 300 ? body[..300] : body)}");

    private sealed record UserBody(
        [property: JsonPropertyName("email"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Email,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("email_confirm")] bool EmailConfirm);

    private sealed record SignInBody([property: JsonPropertyName("email")] string Email, [property: JsonPropertyName("password")] string Password);

    private sealed record UserResponse([property: JsonPropertyName("id")] Guid Id, [property: JsonPropertyName("email")] string? Email);

    private sealed record UserList([property: JsonPropertyName("users")] List<UserResponse> Users);

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
