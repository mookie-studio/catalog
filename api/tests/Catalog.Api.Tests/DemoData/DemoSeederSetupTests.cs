using System.Net;
using System.Text;
using System.Text.Json;
using Catalog.DemoSeeder;
using Catalog.DemoSeeder.Dataset;

namespace Catalog.Api.Tests.DemoData;

/// <summary>
/// The Demo seeder's own boundaries: the command line, its settings, the Demo dataset files, and the HTTP
/// requests it sends to Supabase Auth (the identity provider is faked in the end-to-end seeder tests).
/// </summary>
public sealed class DemoSeederSetupTests : IDisposable
{
    private readonly string _root = TestDataset.Write();

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void The_test_dataset_loads()
    {
        var dataset = DatasetLoader.Load(_root);

        Assert.Equal([TestDataset.Shop, TestDataset.Closed], dataset.Tenants.Select(t => t.Slug));
        Assert.Equal(3, dataset.Tenants[0].Products.Count);
        Assert.Equal(["Tools", "Tools/Drills", "Tools/Saws", "Paint"], dataset.Tenants[0].Catalog.CategoryPaths());
    }

    public static TheoryData<string, object, string> InvalidProducts => new()
    {
        { "duplicate SKU", new object[] { Product("A"), Product("A") }, "products.json $[1].sku: duplicate SKU 'A'" },
        { "unknown Category", new object[] { Product("A", categories: ["Garden"]) }, "$[0].categories: 'Garden' is not a Category" },
        { "unknown attribute", new object[] { Product("A", attributes: new() { ["Colour"] = "Red" }) }, "$[0].attributes.Colour: not an Attribute Definition" },
        { "not an Option", new object[] { Product("A", attributes: new() { ["Voltage"] = "230 V" }) }, "$[0].attributes.Voltage: expected one Option" },
        { "single for multi", new object[] { Product("A", attributes: new() { ["Sizes"] = "S" }) }, "$[0].attributes.Sizes: expected a non-empty array" },
        { "text for number", new object[] { Product("A", attributes: new() { ["Power"] = "500" }) }, "$[0].attributes.Power: expected a number" },
        { "string for boolean", new object[] { Product("A", attributes: new() { ["Cordless"] = "yes" }) }, "$[0].attributes.Cordless: expected true or false" },
        { "bad date", new object[] { Product("A", attributes: new() { ["Released"] = "01/03/2025" }) }, "$[0].attributes.Released: expected a date" },
        { "empty text", new object[] { Product("A", attributes: new() { ["Material"] = " " }) }, "$[0].attributes.Material: expected a non-empty string" },
        { "bad status", new object[] { Product("A", status: "Live") }, "$[0].status: must be Draft, Published or Archived" },
        { "Published without description", new object[] { Product("A", description: null) }, "$[0].description: a Published product needs a description" },
        { "too many images", new object[] { Product("A", images: ["one.jpg", "one.jpg", "one.jpg", "one.jpg", "one.jpg", "one.jpg"]) }, "$[0].images: at most 5 Images" },
        { "missing image", new object[] { Product("A", images: ["missing.jpg"]) }, "$[0].images[0]: 'missing.jpg' was not found" },
        { "not an image", new object[] { Product("A", images: ["notes.txt"]) }, "$[0].images[0]: 'notes.txt' must be a .jpg, .png or .webp file" },
    };

    [Theory]
    [MemberData(nameof(InvalidProducts))]
    public void An_invalid_product_is_reported_with_its_file_and_path(string rule, object products, string expected)
    {
        TestDataset.Save(_root, $"{TestDataset.Shop}/products.json", products);
        File.Delete(Path.Combine(_root, TestDataset.Shop, "activity.json"));

        var error = Assert.Throws<DatasetException>(() => DatasetLoader.Load(_root));

        Assert.True(error.Errors.Any(e => e.Contains(expected, StringComparison.Ordinal)), $"{rule}: {string.Join(" | ", error.Errors)}");
    }

    [Fact]
    public void An_image_over_2_MB_is_reported()
    {
        File.WriteAllBytes(Path.Combine(_root, "images", TestDataset.Shop, "one.jpg"), new byte[2 * 1024 * 1024 + 1]);

        var error = Assert.Throws<DatasetException>(() => DatasetLoader.Load(_root));

        Assert.Contains(error.Errors, e => e.Contains("'one.jpg' is larger than 2 MB"));
    }

    [Fact]
    public void Activity_must_name_Published_products_and_valid_statuses()
    {
        TestDataset.Save(_root, $"{TestDataset.Shop}/activity.json", new
        {
            inquiries = new object[] { new { sku = "DS-3", name = "A", email = "a@example.com", message = "Hi", status = "Closed", daysAgo = -1 } },
            views = new { days = 0, minPerDay = 3, maxPerDay = 1 },
        });

        var error = Assert.Throws<DatasetException>(() => DatasetLoader.Load(_root));

        Assert.Contains(error.Errors, e => e.Contains("$.inquiries[0].sku: 'DS-3' is not a Published product"));
        Assert.Contains(error.Errors, e => e.Contains("$.inquiries[0].status: must be Open or Handled"));
        Assert.Contains(error.Errors, e => e.Contains("$.inquiries[0].daysAgo"));
        Assert.Contains(error.Errors, e => e.Contains("$.views: days must be 1 to 366"));
    }

    [Fact]
    public void Duplicate_Tenants_and_a_missing_logo_are_reported()
    {
        TestDataset.Save(_root, "tenants.json", new object[]
        {
            new { slug = "same", name = "One", isActive = true, adminEmail = "one@example.com" },
            new { slug = "same", name = "Two", isActive = true, adminEmail = "one@example.com" },
        });
        TestDataset.Save(_root, "same/profile.json", new { logo = "nope.png" });

        var error = Assert.Throws<DatasetException>(() => DatasetLoader.Load(_root));

        Assert.Contains(error.Errors, e => e.Contains("$[1].slug: duplicate Tenant slug 'same'"));
        Assert.Contains(error.Errors, e => e.Contains("$[1].adminEmail"));
        Assert.Contains(error.Errors, e => e.Contains("same/profile.json $.logo: 'nope.png' was not found"));
    }

    [Fact]
    public void Every_missing_setting_is_named()
    {
        var errors = new DemoOptions { AdminPassword = "short" }.Validate();

        Assert.Equal(
        [
            "Demo__ApiUrl is not set.",
            "Demo__SupabaseUrl is not set.",
            "Demo__SupabaseAnonKey is not set.",
            "Demo__SupabaseServiceRoleKey is not set.",
            "Demo__DatabaseConnectionString is not set.",
            "Demo__PlatformAdminEmail is not set.",
            "Demo__PlatformAdminPassword is not set.",
            "Demo__AdminPassword must be at least 12 characters long.",
        ], errors);
    }

    [Theory]
    [InlineData(new[] { "seed" }, true)]
    [InlineData(new[] { "seed", "--dataset", "x", "--skip-activity", "--force-images" }, true)]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "reset" }, false)]
    [InlineData(new[] { "seed", "--dataset" }, false)]
    [InlineData(new[] { "seed", "--unknown" }, false)]
    public void The_command_line_is_parsed(string[] args, bool valid)
    {
        Assert.Equal(valid, SeedArguments.Parse(args) is not null);
    }

    [Fact]
    public async Task A_new_demo_account_is_created_confirmed_with_its_password()
    {
        var id = Guid.CreateVersion7();
        var handler = new Stub((HttpStatusCode.OK, $$"""{"id":"{{id}}","email":"a@example.com"}"""));

        var result = await Identity(handler).EnsureUserAsync("a@example.com", "password-123456", CancellationToken.None);

        Assert.Equal(id, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST https://project.supabase.test/auth/v1/admin/users", request.Line);
        Assert.Equal("service-role-key", request.ApiKey);
        Assert.Equal("Bearer service-role-key", request.Authorization);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("a@example.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("password-123456", body.RootElement.GetProperty("password").GetString());
        Assert.True(body.RootElement.GetProperty("email_confirm").GetBoolean());
    }

    [Fact]
    public async Task An_existing_demo_account_is_found_and_its_password_set_again()
    {
        var id = Guid.CreateVersion7();
        var handler = new Stub(
            (HttpStatusCode.UnprocessableEntity, """{"code":422,"error_code":"email_exists","msg":"A user with this email address has already been registered"}"""),
            (HttpStatusCode.OK, $$"""{"users":[{"id":"{{Guid.CreateVersion7()}}","email":"other@example.com"},{"id":"{{id}}","email":"A@example.com"}]}"""),
            (HttpStatusCode.OK, $$"""{"id":"{{id}}"}"""));

        var result = await Identity(handler).EnsureUserAsync("a@example.com", "password-123456", CancellationToken.None);

        Assert.Equal(id, result);
        Assert.Equal(
        [
            "POST https://project.supabase.test/auth/v1/admin/users",
            "GET https://project.supabase.test/auth/v1/admin/users?page=1&per_page=100",
            $"PUT https://project.supabase.test/auth/v1/admin/users/{id}",
        ], handler.Requests.Select(r => r.Line));
        using var body = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal("password-123456", body.RootElement.GetProperty("password").GetString());
        Assert.False(body.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Sign_in_uses_the_anon_key_and_returns_the_access_token()
    {
        var handler = new Stub((HttpStatusCode.OK, """{"access_token":"token-1","token_type":"bearer"}"""));

        var token = await Identity(handler).SignInAsync("a@example.com", "password-123456", CancellationToken.None);

        Assert.Equal("token-1", token);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST https://project.supabase.test/auth/v1/token?grant_type=password", request.Line);
        Assert.Equal("anon-key", request.ApiKey);
        Assert.Null(request.Authorization);
    }

    [Fact]
    public async Task A_secret_key_is_sent_on_apikey_only()
    {
        var handler = new Stub((HttpStatusCode.OK, $$"""{"id":"{{Guid.CreateVersion7()}}"}"""));
        var identity = new SupabaseDemoIdentity(new HttpClient(handler), "https://project.supabase.test", "sb_secret_abc", "anon-key");

        await identity.EnsureUserAsync("a@example.com", "password-123456", CancellationToken.None);

        Assert.Equal("sb_secret_abc", handler.Requests[0].ApiKey);
        Assert.Null(handler.Requests[0].Authorization);
    }

    [Fact]
    public async Task A_Supabase_failure_names_the_account_but_not_the_password()
    {
        var handler = new Stub((HttpStatusCode.BadRequest, """{"msg":"Password should be at least 6 characters"}"""));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Identity(handler).EnsureUserAsync("a@example.com", "secret-password-1", CancellationToken.None));

        Assert.Contains("a@example.com", error.Message);
        Assert.Contains("400", error.Message);
        Assert.DoesNotContain("secret-password-1", error.Message);
    }

    private static SupabaseDemoIdentity Identity(Stub handler) =>
        new(new HttpClient(handler), "https://project.supabase.test/", "service-role-key", "anon-key");

    private static object Product(
        string sku, string status = "Published", string? description = "Text.", string[]? categories = null,
        Dictionary<string, object>? attributes = null, string[]? images = null) =>
        new { sku, name = sku, description, status, featured = false, categories, attributes, images };

    private sealed record Recorded(string Line, string? ApiKey, string? Authorization, string? Body);

    private sealed class Stub(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        private int _next;

        public List<Recorded> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(new Recorded(
                $"{request.Method} {request.RequestUri}",
                request.Headers.TryGetValues("apikey", out var key) ? key.Single() : null,
                request.Headers.Authorization?.ToString(),
                request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));
            var (status, body) = responses[_next++];
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
