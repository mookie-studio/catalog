using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

public static class ListProducts
{
    public sealed record Item(Guid Id, string Name, string Sku, string Slug, ProductStatus Status, DateTimeOffset UpdatedAt, bool IsFeatured);

    public static RouteGroupBuilder MapListProducts(this RouteGroupBuilder admin)
    {
        admin.MapGet("/products", Handle).WithName("ListProducts");
        return admin;
    }

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        int? page, int? pageSize, string? q, ProductStatus? status, Guid? categoryId, bool? featured, bool? hasImages, bool? hasCategories,
        CatalogDbContext db,
        CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{EscapeLike(q.Trim())}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern, "\\") || EF.Functions.ILike(p.Sku, pattern, "\\"));
        }

        if (categoryId is { } category)
        {
            query = query.Where(p => db.ProductCategories.Any(l => l.ProductId == p.Id && l.CategoryId == category));
        }

        if (status is { } s)
        {
            query = query.Where(p => p.Status == s);
        }

        if (featured is { } f)
        {
            query = query.Where(p => p.IsFeatured == f);
        }

        if (hasImages is { } withImages)
        {
            query = query.Where(p => db.ProductImages.Any(i => i.ProductId == p.Id) == withImages);
        }

        if (hasCategories is { } withCategories)
        {
            query = query.Where(p => db.ProductCategories.Any(l => l.ProductId == p.Id) == withCategories);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(p => new Item(p.Id, p.Name, p.Sku, p.Slug, p.Status, p.UpdatedAt, p.IsFeatured))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<Item>(items, paging.Page, paging.PageSize, totalCount));
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
