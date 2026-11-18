using System.Net;
using System.Net.Http.Json;
using Catalog.DemoSeeder;

namespace Catalog.Api.Tests.DemoData;

/// <summary>The Demo seeder run against the in-process API and the test database; assertions go through HTTP.</summary>
public sealed class DemoSeederTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private readonly string _root = TestDataset.Write();
    private readonly FakeDemoIdentity _identity = new(TestTokens.PlatformAdminUserId);

    private async Task<SeedLog> Seed(bool skipActivity = false, bool forceImages = false)
    {
        var context = TestDataset.Context(Factory, _root, new StringWriter(), _identity, skipActivity: skipActivity, forceImages: forceImages);
        await Seeder.RunAsync(context, CancellationToken.None);
        return context.Log;
    }

    private async Task<HttpClient> ShopAdmin()
    {
        var platform = PlatformAdminClient();
        var tenant = (await platform.GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=50"))!
            .Items.Single(t => t.Slug == TestDataset.Shop);
        var admin = (await platform.GetFromJsonAsync<PagedBody<AdminBody>>($"/api/platform/tenants/{tenant.Id}/admins"))!.Items.Single();
        return ClientFor(admin.UserId, admin.Email);
    }

    private async Task<Dictionary<string, ProductListItem>> Products(HttpClient admin) =>
        (await admin.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products?pageSize=50"))!.Items.ToDictionary(p => p.Sku);

    [Fact]
    public async Task A_first_run_creates_the_Demo_tenants_and_their_catalogs()
    {
        await Seed();

        var tenants = (await PlatformAdminClient().GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=50"))!.Items;
        Assert.True(tenants.Single(t => t.Slug == TestDataset.Shop).IsActive);
        Assert.False(tenants.Single(t => t.Slug == TestDataset.Closed).IsActive);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/public/{TestDataset.Closed}")).StatusCode);

        var admin = await ShopAdmin();
        Assert.Equal(TestDataset.Shop, (await admin.GetFromJsonAsync<CurrentTenantBody>("/api/admin/tenant"))!.Slug);

        var profile = await Client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/public/{TestDataset.Shop}");
        Assert.Equal("A shop for the demo.", profile.GetProperty("description").GetString());
        Assert.Equal("#B45309", profile.GetProperty("brandColor").GetString());
        Assert.False(string.IsNullOrEmpty(profile.GetProperty("logoUrl").GetString()));

        var categories = (await admin.GetFromJsonAsync<List<CategoryBody>>("/api/admin/categories"))!;
        Assert.Equal(["Tools", "Paint"], categories.Select(c => c.Name));
        Assert.Equal(["Drills", "Saws"], categories[0].Children.Select(c => c.Name));

        var products = await Products(admin);
        Assert.Equal(["DS-1", "DS-2", "DS-3"], products.Keys.Order());
        Assert.Equal("Published", products["DS-1"].Status);
        Assert.Equal("Draft", products["DS-3"].Status);

        var drill = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/admin/products/{products["DS-1"].Id}");
        Assert.True(drill.GetProperty("isFeatured").GetBoolean());
        Assert.Equal(6, drill.GetProperty("attributes").GetArrayLength());
        Assert.Equal(2, drill.GetProperty("images").GetArrayLength());
        Assert.Single(drill.GetProperty("categoryIds").EnumerateArray());

        var page = (await Client.GetFromJsonAsync<PublicProductPage>($"/api/public/{TestDataset.Shop}/products"))!;
        Assert.Equal(2, page.TotalCount);
        Assert.Contains(page.Items, p => p.Name == "Cordless drill" && p.MainImageUrl is not null);

        var dashboard = (await admin.GetFromJsonAsync<DashboardBody>("/api/admin/dashboard"))!;
        Assert.Equal(1, dashboard.OpenInquiryCount);
        Assert.Equal(1, dashboard.FeaturedCount);
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        await Seed();

        var second = await Seed();

        Assert.Equal(0, second.TotalChanges);
    }

    [Fact]
    public async Task A_run_undoes_what_was_changed_through_the_API()
    {
        await Seed();
        var admin = await ShopAdmin();
        var products = await Products(admin);
        var drill = products["DS-1"].Id;

        await admin.PutAsJsonAsync($"/api/admin/products/{drill}", new { name = "Renamed", description = "Changed.", sku = "DS-1", slug = products["DS-1"].Slug });
        await admin.PutAsJsonAsync($"/api/admin/products/{drill}/featured", new { featured = false });
        await admin.PostAsJsonAsync($"/api/admin/products/{products["DS-2"].Id}/status", new { status = "Archived" });
        var image = (await admin.GetFromJsonAsync<ProductImagesBody>($"/api/admin/products/{drill}"))!.Images[0];
        await admin.DeleteAsync($"/api/admin/products/{drill}/images/{image.Id}");
        var extra = await CreatePublishedProduct(admin, "Visitor-made product", "EXTRA-1");
        await CreateCategory(admin, "Garden");
        await CreateDefinition(admin, "Colour", "Text");
        await admin.PutAsJsonAsync("/api/admin/tenant/profile", new { description = "Edited during the demo." });

        var run = await Seed(skipActivity: true);

        Assert.True(run.TotalChanges > 0);
        var after = await Products(admin);
        Assert.Equal("Cordless drill", after["DS-1"].Name);
        Assert.Equal("Published", after["DS-2"].Status);
        Assert.Equal("Archived", after["EXTRA-1"].Status);
        Assert.Equal(extra.Id, after["EXTRA-1"].Id);
        var detail = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/admin/products/{drill}");
        Assert.True(detail.GetProperty("isFeatured").GetBoolean());
        Assert.Equal(2, detail.GetProperty("images").GetArrayLength());
        Assert.Equal(["Tools", "Paint"], (await admin.GetFromJsonAsync<List<CategoryBody>>("/api/admin/categories"))!.Select(c => c.Name));
        var definitions = (await admin.GetFromJsonAsync<PagedBody<DefinitionBody>>("/api/admin/attribute-definitions?pageSize=50"))!.Items;
        Assert.DoesNotContain(definitions, d => d.Name == "Colour");
        var profile = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/admin/tenant/profile");
        Assert.Equal("A shop for the demo.", profile.GetProperty("description").GetString());

        Assert.Equal(0, (await Seed(skipActivity: true)).TotalChanges);
    }

    [Fact]
    public async Task Force_images_uploads_every_Image_again()
    {
        await Seed();

        var run = await Seed(forceImages: true);

        Assert.True(run.TotalChanges >= 4);
    }

    [Fact]
    public async Task A_Tenant_with_a_dataset_slug_and_other_Admins_stops_the_run_before_any_change()
    {
        await CreateTenantWithAdmin(TestDataset.Shop, "someone@real.example.com");

        var error = await Assert.ThrowsAsync<SeedException>(() => Seed());

        Assert.Contains($"Tenant '{TestDataset.Shop}' exists and is not a Demo tenant", error.Message);
        var tenants = (await PlatformAdminClient().GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=50"))!.Items;
        Assert.DoesNotContain(tenants, t => t.Slug == TestDataset.Closed);
    }

    [Fact]
    public async Task A_Tenant_that_is_not_in_the_dataset_is_left_untouched()
    {
        var other = await CreateTenantWithAdmin("real-business");
        var product = await CreatePublishedProduct(other.Client, "Real product", "R-1");
        (await Client.PostAsJsonAsync($"/api/public/real-business/products/{product.Slug}/views", new { })).EnsureSuccessStatusCode();
        await other.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true });
        (await Client.PostAsJsonAsync($"/api/public/real-business/products/{product.Slug}/inquiries",
            new { name = "Eva", email = "eva@example.com", message = "Hello there" })).EnsureSuccessStatusCode();

        await Seed();

        var products = (await other.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products"))!.Items;
        Assert.Equal("Published", Assert.Single(products).Status);
        Assert.Equal(1, (await other.Client.GetFromJsonAsync<DashboardBody>("/api/admin/dashboard"))!.OpenInquiryCount);
        var stats = (await other.Client.GetFromJsonAsync<PagedBody<ProductStatBody>>("/api/admin/stats/products"))!.Items;
        Assert.Equal(1, Assert.Single(stats).Views);
        var tenants = (await PlatformAdminClient().GetFromJsonAsync<PagedBody<TenantBody>>("/api/platform/tenants?pageSize=50"))!.Items;
        Assert.True(tenants.Single(t => t.Slug == "real-business").IsActive);
    }

    [Fact]
    public async Task Demo_activity_is_back_dated_deterministic_and_rewritten_on_every_run()
    {
        await Seed();
        var admin = await ShopAdmin();
        var drill = (await Products(admin))["DS-1"];
        (await Client.PostAsJsonAsync($"/api/public/{TestDataset.Shop}/products/{drill.Slug}/inquiries",
            new { name = "Visitor", email = "visitor@example.com", message = "During the demo" })).EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var range = $"/api/admin/stats/products?from={today.AddDays(-29):yyyy-MM-dd}&to={today:yyyy-MM-dd}";
        var before = (await admin.GetFromJsonAsync<PagedBody<ProductStatBody>>(range))!.Items;

        await Seed();

        var inquiries = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/admin/inquiries");
        Assert.Equal(2, inquiries.GetProperty("totalCount").GetInt32());
        var after = (await admin.GetFromJsonAsync<PagedBody<ProductStatBody>>(range))!.Items;
        Assert.Equal(before.Select(s => (s.Name, s.Views)), after.Select(s => (s.Name, s.Views)));
        Assert.Equal(["Cordless drill", "Hand saw"], after.Select(s => s.Name));
        Assert.True(after[0].Views >= 30 * 3);

        var lastWeek = (await admin.GetFromJsonAsync<PagedBody<ProductStatBody>>(
            $"/api/admin/stats/products?from={today.AddDays(-29):yyyy-MM-dd}&to={today.AddDays(-7):yyyy-MM-dd}"))!.Items;
        Assert.Equal(1, lastWeek.Single(s => s.Name == "Hand saw").Inquiries);
    }

    [Fact]
    public async Task A_run_stops_when_the_demo_Platform_Admin_is_not_configured_in_the_API()
    {
        var unknown = Guid.CreateVersion7();
        var context = TestDataset.Context(Factory, _root, new StringWriter(), new FakeDemoIdentity(unknown));

        var error = await Assert.ThrowsAsync<SeedException>(() => Seeder.RunAsync(context, CancellationToken.None));

        Assert.Contains($"(user id {unknown}) is not a Platform Admin", error.Message);
        Assert.Contains("Auth:PlatformAdminUserIds", error.Message);
    }

    public override Task DisposeAsync()
    {
        Directory.Delete(_root, recursive: true);
        return Task.CompletedTask;
    }
}
