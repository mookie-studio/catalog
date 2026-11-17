using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class FeaturedAndSortTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static Task<HttpResponseMessage> Feature(HttpClient client, Guid id, bool featured) =>
        client.PutAsJsonAsync($"/api/admin/products/{id}/featured", new { featured });

    private static Task Publish(HttpClient client, Guid id) =>
        client.PostAsJsonAsync($"/api/admin/products/{id}/status", new { status = "Published" });

    [Fact]
    public async Task Featuring_and_unfeaturing_persist()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer drill", "HD-1");

        var featured = await (await Feature(a.Client, product.Id, true)).Content.ReadFromJsonAsync<FeaturedProductBody>();
        Assert.True(featured!.IsFeatured);
        Assert.True((await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.IsFeatured);

        await Feature(a.Client, product.Id, false);
        Assert.False((await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.IsFeatured);
    }

    [Fact]
    public async Task The_admin_list_shows_and_filters_Featured_products_with_other_filters()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var drill = await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        var saw = await CreateProduct(a.Client, "Circular saw", "CS-1");
        await CreatePublishedProduct(a.Client, "Plain hammer", "PH-1");
        await Feature(a.Client, drill.Id, true);
        await Feature(a.Client, saw.Id, true);

        var featured = await a.Client.GetFromJsonAsync<PagedBody<AdminListItem>>("/api/admin/products?featured=true&status=Published");
        var notFeatured = await a.Client.GetFromJsonAsync<PagedBody<AdminListItem>>("/api/admin/products?featured=false");

        Assert.Equal(["Hammer drill"], featured!.Items.Select(p => p.Name));
        Assert.True(featured.Items[0].IsFeatured);
        Assert.Equal(["Plain hammer"], notFeatured!.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Isolation_tenant_A_gets_404_featuring_tenant_Bs_product()
    {
        await CreateTenantWithAdmin("tenant-a");
        var a = await CreateTenantWithAdmin("tenant-c");
        var b = await CreateTenantWithAdmin("tenant-b");
        var product = await CreateProduct(b.Client, "Hammer drill", "HD-1");

        Assert.Equal(HttpStatusCode.NotFound, (await Feature(a.Client, product.Id, true)).StatusCode);
        Assert.False((await b.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.IsFeatured);
    }

    [Fact]
    public async Task Publishing_sets_PublishedAt_and_republishing_updates_it()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var product = await CreateProduct(a.Client, "Hammer drill", "HD-1", "Description");
        Assert.Null((await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.PublishedAt);

        await Publish(a.Client, product.Id);
        var first = (await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.PublishedAt;
        await a.Client.PostAsJsonAsync($"/api/admin/products/{product.Id}/status", new { status = "Archived" });
        var archived = (await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.PublishedAt;
        await Publish(a.Client, product.Id);
        var second = (await a.Client.GetFromJsonAsync<FeaturedProductBody>($"/api/admin/products/{product.Id}"))!.PublishedAt;

        Assert.NotNull(first);
        Assert.Equal(first, archived);
        Assert.True(second > first);
    }

    private async Task<AdminSession> Catalog()
    {
        // Published in this order: Bravo, Alpha, Delta, Charlie. Charlie and Delta are Featured.
        var a = await CreateTenantWithAdmin("tenant-a");
        var ids = new Dictionary<string, Guid>();
        foreach (var name in new[] { "Bravo drill", "Alpha drill", "Delta saw", "Charlie saw" })
        {
            ids[name] = (await CreatePublishedProduct(a.Client, name, name.ToUpperInvariant().Replace(' ', '-'),
                name.Contains("drill") ? "A drill for walls." : "A saw for wood.")).Id;
        }

        await Feature(a.Client, ids["Charlie saw"], true);
        await Feature(a.Client, ids["Delta saw"], true);
        return a;
    }

    private async Task<List<string>> Names(string query) =>
        (await Client.GetFromJsonAsync<PagedBody<PublicItem>>($"/api/public/tenant-a/products{query}"))!.Items.Select(i => i.Name).ToList();

    [Fact]
    public async Task Each_sort_order_follows_its_rule()
    {
        await Catalog();

        Assert.Equal(["Charlie saw", "Delta saw", "Alpha drill", "Bravo drill"], await Names(""));
        Assert.Equal(["Charlie saw", "Delta saw", "Alpha drill", "Bravo drill"], await Names("?sort=featured"));
        Assert.Equal(["Alpha drill", "Bravo drill", "Charlie saw", "Delta saw"], await Names("?sort=name"));
        Assert.Equal(["Charlie saw", "Delta saw", "Alpha drill", "Bravo drill"], await Names("?sort=newest"));
        Assert.Equal(["Charlie saw", "Delta saw", "Alpha drill", "Bravo drill"], await Names("?sort=relevance"));
    }

    [Fact]
    public async Task Newest_uses_the_last_publication()
    {
        var a = await Catalog();
        var list = await a.Client.GetFromJsonAsync<PagedBody<AdminListItem>>("/api/admin/products?q=Bravo");
        var bravo = list!.Items[0].Id;
        await a.Client.PostAsJsonAsync($"/api/admin/products/{bravo}/status", new { status = "Draft" });
        await Publish(a.Client, bravo);

        Assert.Equal("Bravo drill", (await Names("?sort=newest"))[0]);
    }

    [Fact]
    public async Task Search_defaults_to_relevance_and_accepts_another_order()
    {
        var a = await Catalog();
        await a.Client.PutAsJsonAsync($"/api/admin/products/{(await a.Client.GetFromJsonAsync<PagedBody<AdminListItem>>("/api/admin/products?q=Alpha"))!.Items[0].Id}",
            new { name = "Alpha drill", sku = "ALPHA-DRILL", description = "A saw." , slug = "alpha-drill" });

        var relevance = await Names("?q=drill");
        Assert.Equal(["Bravo drill", "Alpha drill"], relevance);
        Assert.Equal(["Alpha drill", "Bravo drill"], await Names("?q=drill&sort=name"));
    }

    [Fact]
    public async Task Featured_true_combines_with_search_and_category()
    {
        var a = await Catalog();
        var category = await CreateCategory(a.Client, "Saws");
        var charlie = (await a.Client.GetFromJsonAsync<PagedBody<AdminListItem>>("/api/admin/products?q=Charlie"))!.Items[0].Id;
        await a.Client.PutAsJsonAsync($"/api/admin/products/{charlie}/categories", new { categoryIds = new[] { category.Id } });

        var items = (await Client.GetFromJsonAsync<PagedBody<PublicItem>>("/api/public/tenant-a/products?featured=true"))!.Items;
        Assert.Equal(["Charlie saw", "Delta saw"], items.Select(i => i.Name));
        Assert.All(items, i => Assert.True(i.IsFeatured));
        Assert.Equal(["Charlie saw"], await Names($"?featured=true&categoryId={category.Id}"));
        Assert.Equal(["Charlie saw", "Delta saw"], await Names("?featured=true&q=saw"));
        Assert.Empty(await Names("?featured=true&q=drill"));
    }

    [Fact]
    public async Task An_unknown_sort_returns_400()
    {
        await Catalog();

        var response = await Client.GetAsync("/api/public/tenant-a/products?sort=price");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["sort"], problem!.Errors.Keys);
    }

    private sealed record FeaturedProductBody(Guid Id, bool IsFeatured, DateTimeOffset? PublishedAt);

    private sealed record AdminListItem(Guid Id, string Name, bool IsFeatured);

    private sealed record PublicItem(string Slug, string Name, string? MainImageUrl, bool IsFeatured);
}
