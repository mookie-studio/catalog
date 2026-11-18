using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Catalog.DemoSeeder;

/// <summary>A Catalog API request failed; the message has the method, path, status and the ProblemDetails.</summary>
public sealed class ApiException(string message, HttpStatusCode status) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>Calls the Catalog API as one signed-in person (a Platform Admin or an Admin).</summary>
public sealed class ApiClient(HttpClient http, string accessToken)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<T> GetAsync<T>(string path, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, ct);

    public Task<T> PostAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Post, path, Body(body), ct);

    public Task<T> PutAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Put, path, Body(body), ct);

    public Task PostAsync(string path, object? body, CancellationToken ct) => SendAsync<object?>(HttpMethod.Post, path, Body(body), ct);

    public Task PutAsync(string path, object? body, CancellationToken ct) => SendAsync<object?>(HttpMethod.Put, path, Body(body), ct);

    public Task DeleteAsync(string path, CancellationToken ct) => SendAsync<object?>(HttpMethod.Delete, path, null, ct);

    /// <summary>Uploads a file as the multipart field "file", the way the SPA does.</summary>
    public Task<T> UploadAsync<T>(HttpMethod method, string path, string filePath, CancellationToken ct)
    {
        var file = new ByteArrayContent(File.ReadAllBytes(filePath));
        file.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeOf(filePath));
        var form = new MultipartFormDataContent { { file, "file", Path.GetFileName(filePath) } };
        return SendAsync<T>(method, path, form, ct);
    }

    /// <summary>Every item of a paginated list (<c>{ items, page, pageSize, totalCount }</c>), 50 per request.</summary>
    public async Task<List<T>> GetAllPagesAsync<T>(string path, CancellationToken ct)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var items = new List<T>();
        for (var page = 1; ; page++)
        {
            var result = await GetAsync<Paged<T>>($"{path}{separator}page={page}&pageSize=50", ct);
            items.AddRange(result.Items);
            if (result.Items.Count == 0 || items.Count >= result.TotalCount)
            {
                return items;
            }
        }
    }

    public static string ContentTypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg",
    };

    private static JsonContent? Body(object? body) => body is null ? null : JsonContent.Create(body, options: Json);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException($"{method} {path} answered {(int)response.StatusCode} {response.StatusCode}: {Describe(text)}", response.StatusCode);
        }

        if (typeof(T) == typeof(object) || response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    private static string Describe(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var errors = root.TryGetProperty("errors", out var e) ? e.GetRawText() : null;
            return string.Join(" ", new[] { title, errors }.Where(s => !string.IsNullOrEmpty(s)));
        }
        catch (JsonException)
        {
            return body.Length > 300 ? body[..300] : body;
        }
    }
}

public sealed record Paged<T>(List<T> Items, int Page, int PageSize, int TotalCount);
