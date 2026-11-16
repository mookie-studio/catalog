using System.Net.Http.Json;

namespace Catalog.Api.Tests;

public sealed class PublicSearchTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Spanish_stemming_finds_singular_names_from_plural_searches()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Taladro percutor", "T-1", "Potente y ligero");
        await CreatePublishedProduct(a.Client, "Sierra circular", "S-1", "Corte preciso");

        var page = await Search("taladros");

        Assert.Equal(["Taladro percutor"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Name_matches_rank_above_description_matches()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Aaa kit de brocas", "K-1", "Incluye accesorios para el taladro");
        await CreatePublishedProduct(a.Client, "Taladro compacto", "T-1", "Ligero");

        var page = await Search("taladro");

        Assert.Equal(["Taladro compacto", "Aaa kit de brocas"], page.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Only_Published_products_are_searched()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreateProduct(a.Client, "Taladro borrador", "T-1", "Borrador");

        Assert.Empty((await Search("taladro")).Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20%20")]
    public async Task An_empty_q_behaves_like_no_search(string q)
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await CreatePublishedProduct(a.Client, "Sierra", "S-1");
        await CreatePublishedProduct(a.Client, "Martillo", "M-1");

        var page = await Search(q);

        Assert.Equal(["Martillo", "Sierra"], page.Items.Select(p => p.Name));
    }

    private async Task<PagedBody<PublicProductItem>> Search(string q) =>
        (await Client.GetFromJsonAsync<PagedBody<PublicProductItem>>($"/api/public/tenant-a/products?q={q}"))!;
}
