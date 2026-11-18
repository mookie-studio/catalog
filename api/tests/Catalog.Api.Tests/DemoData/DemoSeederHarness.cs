using System.Text.Json;
using Catalog.DemoSeeder;
using Catalog.DemoSeeder.Dataset;

namespace Catalog.Api.Tests.DemoData;

/// <summary>The identity provider fake for the Demo seeder: stable ids per email and minted test tokens.</summary>
public sealed class FakeDemoIdentity(Guid platformAdminUserId) : IDemoIdentity
{
    public const string PlatformAdminEmail = "platform@demo.test";

    private readonly Dictionary<string, Guid> _users = new(StringComparer.OrdinalIgnoreCase)
    {
        [PlatformAdminEmail] = platformAdminUserId,
    };

    public Task<Guid> EnsureUserAsync(string email, string password, CancellationToken ct)
    {
        if (!_users.TryGetValue(email, out var id))
        {
            id = _users[email] = Guid.CreateVersion7();
        }

        return Task.FromResult(id);
    }

    public Task<string> SignInAsync(string email, string password, CancellationToken ct) =>
        Task.FromResult(TestTokens.Create(_users[email], email));
}

/// <summary>Writes a small Demo dataset to a temporary folder: one active and one deactivated Demo tenant.</summary>
public static class TestDataset
{
    public const string Shop = "demo-shop";
    public const string Closed = "demo-closed";
    public const string ShopAdmin = "admin@demo-shop.example.com";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string Write()
    {
        var root = Path.Combine(Path.GetTempPath(), "catalog-demo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Save(root, "tenants.json", new object[]
        {
            new { slug = Shop, name = "Demo Shop", isActive = true, adminEmail = ShopAdmin },
            new { slug = Closed, name = "Demo Closed", isActive = false, adminEmail = "admin@demo-closed.example.com" },
        });

        var images = Path.Combine(root, "images", Shop);
        Directory.CreateDirectory(images);
        File.WriteAllBytes(Path.Combine(images, "logo.png"), TestImages.Png());
        File.WriteAllBytes(Path.Combine(images, "one.jpg"), TestImages.Jpeg());
        File.WriteAllBytes(Path.Combine(images, "two.jpg"), TestImages.Jpeg(80, 60));

        Save(root, $"{Shop}/profile.json", new
        {
            description = "A shop for the demo.",
            contactEmail = "hello@demo-shop.example.com",
            phone = "+34 600 000 000",
            address = "1 Demo Street",
            websiteUrl = "https://demo-shop.example.com",
            brandColor = "#B45309",
            inquiriesEnabled = true,
            logo = "logo.png",
        });
        Save(root, $"{Shop}/catalog.json", new
        {
            categories = new object[]
            {
                new { name = "Tools", children = new object[] { new { name = "Drills" }, new { name = "Saws" } } },
                new { name = "Paint" },
            },
            attributes = new object[]
            {
                new { name = "Power", type = "Number", isFilterable = false, allowsMultiple = false },
                new { name = "Cordless", type = "Boolean", isFilterable = true, allowsMultiple = false },
                new { name = "Material", type = "Text", isFilterable = false, allowsMultiple = false },
                new { name = "Released", type = "Date", isFilterable = false, allowsMultiple = false },
                new { name = "Voltage", type = "OptionList", isFilterable = true, allowsMultiple = false, options = new[] { "12 V", "18 V" } },
                new { name = "Sizes", type = "OptionList", isFilterable = true, allowsMultiple = true, options = new[] { "S", "M", "L" } },
            },
        });
        Save(root, $"{Shop}/products.json", new object[]
        {
            new
            {
                sku = "DS-1", name = "Cordless drill", description = "Drills holes.", status = "Published", featured = true,
                categories = new[] { "Tools/Drills" },
                attributes = new Dictionary<string, object>
                {
                    ["Power"] = 500, ["Cordless"] = true, ["Material"] = "Steel", ["Released"] = "2025-03-01",
                    ["Voltage"] = "18 V", ["Sizes"] = new[] { "S", "L" },
                },
                images = new[] { "one.jpg", "two.jpg" },
            },
            new { sku = "DS-2", name = "Hand saw", description = "Cuts wood.", status = "Published", featured = false, categories = new[] { "Tools/Saws" } },
            new { sku = "DS-3", name = "Paint roller", description = (string?)null, status = "Draft", featured = false, images = new[] { "one.jpg" } },
        });
        Save(root, $"{Shop}/activity.json", new
        {
            inquiries = new object[]
            {
                new { sku = "DS-1", name = "Ana", email = "ana@example.com", message = "Is it in stock?", status = "Open", daysAgo = 2 },
                new { sku = "DS-2", name = "Luis", email = "luis@example.com", message = "Which teeth?", status = "Handled", daysAgo = 10 },
            },
            views = new { days = 30, minPerDay = 1, maxPerDay = 5 },
        });

        Save(root, $"{Closed}/products.json", new object[]
        {
            new { sku = "DC-1", name = "Old product", description = "Still here.", status = "Published", featured = false },
        });
        return root;
    }

    public static void Save(string root, string file, object value)
    {
        var path = Path.Combine(root, file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, Json));
    }

    public static SeedContext Context(
        CatalogApiFactory factory, string root, StringWriter output, IDemoIdentity identity,
        bool skipActivity = false, bool forceImages = false)
    {
        var options = new DemoOptions
        {
            ApiUrl = "http://localhost",
            SupabaseUrl = "http://supabase.test",
            SupabaseAnonKey = "anon",
            SupabaseServiceRoleKey = "service",
            DatabaseConnectionString = factory.ConnectionString,
            PlatformAdminEmail = FakeDemoIdentity.PlatformAdminEmail,
            PlatformAdminPassword = "platform-password",
            AdminPassword = "admin-password",
        };
        return new SeedContext(
            options,
            new SeedArguments(root, skipActivity, forceImages),
            DatasetLoader.Load(root),
            identity,
            () => factory.CreateClient(),
            new SeedLog(output));
    }
}
