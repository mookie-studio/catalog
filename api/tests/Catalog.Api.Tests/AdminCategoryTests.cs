using System.Net;
using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class AdminCategoryTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task A_three_level_tree_is_returned_nested_and_ordered()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var garden = await CreateCategory(a.Client, "Garden");
        var power = await CreateCategory(a.Client, "Power tools", tools.Id);
        var hand = await CreateCategory(a.Client, "Hand tools", tools.Id);
        await CreateCategory(a.Client, "Drills", power.Id);
        await CreateCategory(a.Client, "Saws", power.Id);

        var tree = await Tree(a.Client);

        Assert.Equal(["Tools", "Garden"], tree.Select(c => c.Name));
        Assert.Equal(["Power tools", "Hand tools"], tree[0].Children.Select(c => c.Name));
        Assert.Equal(["Drills", "Saws"], tree[0].Children[0].Children.Select(c => c.Name));
        Assert.Empty(tree[1].Children);
        Assert.Equal(hand.Id, tree[0].Children[1].Id);
        Assert.Equal(garden.Id, tree[1].Id);
    }

    [Fact]
    public async Task Renaming_works()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tols");

        var response = await a.Client.PutAsJsonAsync($"/api/admin/categories/{tools.Id}", new { name = "Tools" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Tools", Assert.Single(await Tree(a.Client)).Name);
    }

    [Fact]
    public async Task An_unknown_parent_or_one_from_another_Tenant_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var foreign = await CreateCategory(b.Client, "Theirs");

        var otherTenant = await a.Client.PostAsJsonAsync("/api/admin/categories", new { name = "Mine", parentId = foreign.Id });
        var unknown = await a.Client.PostAsJsonAsync("/api/admin/categories", new { name = "Mine", parentId = Guid.CreateVersion7() });
        var emptyName = await a.Client.PostAsJsonAsync("/api/admin/categories", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, otherTenant.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyName.StatusCode);
    }

    [Fact]
    public async Task Isolation_categories_of_B_never_appear_for_A_and_are_404()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        var theirs = await CreateCategory(b.Client, "Theirs");
        await CreateCategory(a.Client, "Mine");

        Assert.Equal(["Mine"], (await Tree(a.Client)).Select(c => c.Name));
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.PutAsJsonAsync($"/api/admin/categories/{theirs.Id}", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.PostAsJsonAsync($"/api/admin/categories/{theirs.Id}/move", new { parentId = (Guid?)null, position = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.Client.DeleteAsync($"/api/admin/categories/{theirs.Id}")).StatusCode);
        Assert.Equal(["Theirs"], (await Tree(b.Client)).Select(c => c.Name));
    }

    [Fact]
    public async Task Reordering_siblings_within_the_same_parent_works()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var first = await CreateCategory(a.Client, "A");
        await CreateCategory(a.Client, "B");
        await CreateCategory(a.Client, "C");

        await Move(a.Client, first.Id, null, 2);
        Assert.Equal(["B", "C", "A"], (await Tree(a.Client)).Select(c => c.Name));

        await Move(a.Client, first.Id, null, 1);
        Assert.Equal(["B", "A", "C"], (await Tree(a.Client)).Select(c => c.Name));

        await Move(a.Client, first.Id, null, 99);
        Assert.Equal(["B", "C", "A"], (await Tree(a.Client)).Select(c => c.Name));
    }

    [Fact]
    public async Task Moving_to_another_parent_keeps_both_sibling_lists_contiguous()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var left = await CreateCategory(a.Client, "Left");
        var right = await CreateCategory(a.Client, "Right");
        await CreateCategory(a.Client, "L1", left.Id);
        var l2 = await CreateCategory(a.Client, "L2", left.Id);
        await CreateCategory(a.Client, "L3", left.Id);
        await CreateCategory(a.Client, "R1", right.Id);
        await CreateCategory(a.Client, "R2", right.Id);

        await Move(a.Client, l2.Id, right.Id, 1);
        // Positions are contiguous if appending a new child lands it last, right after the existing ones.
        await CreateCategory(a.Client, "L4", left.Id);
        await CreateCategory(a.Client, "R3", right.Id);

        var tree = await Tree(a.Client);
        Assert.Equal(["L1", "L3", "L4"], tree[0].Children.Select(c => c.Name));
        Assert.Equal(["R1", "L2", "R2", "R3"], tree[1].Children.Select(c => c.Name));
    }

    [Fact]
    public async Task Moving_to_the_root_works()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var drills = await CreateCategory(a.Client, "Drills", tools.Id);

        await Move(a.Client, drills.Id, null, 0);

        Assert.Equal(["Drills", "Tools"], (await Tree(a.Client)).Select(c => c.Name));
    }

    [Fact]
    public async Task A_move_that_would_create_a_cycle_returns_400()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        var power = await CreateCategory(a.Client, "Power", tools.Id);
        var drills = await CreateCategory(a.Client, "Drills", power.Id);

        var underItself = await a.Client.PostAsJsonAsync($"/api/admin/categories/{tools.Id}/move", new { parentId = tools.Id, position = 0 });
        var underDescendant = await a.Client.PostAsJsonAsync($"/api/admin/categories/{tools.Id}/move", new { parentId = drills.Id, position = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, underItself.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, underDescendant.StatusCode);
        Assert.Equal("Tools", Assert.Single(await Tree(a.Client)).Name);
    }

    [Fact]
    public async Task Deleting_a_leaf_keeps_its_products_with_their_other_Categories()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var first = await CreateCategory(a.Client, "First");
        var middle = await CreateCategory(a.Client, "Middle");
        var last = await CreateCategory(a.Client, "Last");
        var product = await CreateProduct(a.Client, "Drill", "D-1");
        (await a.Client.PutAsJsonAsync($"/api/admin/products/{product.Id}/categories", new { categoryIds = new[] { middle.Id, last.Id } }))
            .EnsureSuccessStatusCode();

        var response = await a.Client.DeleteAsync($"/api/admin/categories/{middle.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(["First", "Last"], (await Tree(a.Client)).Select(c => c.Name));
        var detail = await a.Client.GetFromJsonAsync<ProductCategoriesBody>($"/api/admin/products/{product.Id}");
        Assert.Equal([last.Id], detail!.CategoryIds);

        // Sibling positions were compacted: a new root lands right after "Last".
        await Move(a.Client, first.Id, null, 1);
        Assert.Equal(["Last", "First"], (await Tree(a.Client)).Select(c => c.Name));
    }

    [Fact]
    public async Task Deleting_a_Category_with_children_returns_409()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var tools = await CreateCategory(a.Client, "Tools");
        await CreateCategory(a.Client, "Drills", tools.Id);

        var response = await a.Client.DeleteAsync($"/api/admin/categories/{tools.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<List<CategoryBody>> Tree(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CategoryBody>>("/api/admin/categories"))!;

    private static async Task Move(HttpClient client, Guid id, Guid? parentId, int position) =>
        (await client.PostAsJsonAsync($"/api/admin/categories/{id}/move", new { parentId, position })).EnsureSuccessStatusCode();
}
