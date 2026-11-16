using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

public static class ListProducts
{
    public sealed record Item(Guid Id, string Name, string Sku, string Slug, ProductStatus Status, DateTimeOffset UpdatedAt);

    public static RouteGroupBuilder MapListProducts(this RouteGroupBuilder admin)
    {
        admin.MapGet("/products", Handle).WithName("ListProducts");
        return admin;
    }

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        int? page, int? pageSize, string? q, ProductStatus? status, CatalogDbContext db, CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{EscapeLike(q.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\") || EF.Functions.ILike(p.Sku, pattern, "\\"));
        }

        if (status is { } s)
        {
            query = query.Where(p => p.Status == s);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(p => new Item(p.Id, p.Name, p.Sku, p.Slug, p.Status, p.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<Item>(items, paging.Page, paging.PageSize, totalCount));
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
