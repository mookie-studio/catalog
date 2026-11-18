using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Related products (see GLOSSARY.md): other Published products that share a Category with this one, the ones
/// sharing more Categories first, then Featured, name and id.
/// </summary>
public static class ListRelatedProducts
{
    public static RouteGroupBuilder MapListRelatedProducts(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/products/{productSlug}/related", Handle).WithName("ListRelatedProducts");
        return publicGroup;
    }

    private static async Task<Results<Ok<PagedResponse<ListPublicProducts.Item>>, NotFound>> Handle(
        string productSlug, int? page, int? pageSize, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        // An Old Product slug resolves to its product (ADR 0009).
        var productId = await db.Products
            .Where(p => p.Status == ProductStatus.Published)
            .Where(p => p.Slug == productSlug || db.OldProductSlugs.Any(o => o.ProductId == p.Id && o.Slug == productSlug))
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (productId is null)
        {
            return TypedResults.NotFound();
        }

        var paging = Paging.From(page, pageSize);
        var categoryIds = db.ProductCategories.Where(l => l.ProductId == productId).Select(l => l.CategoryId);
        var related = db.Products
            .Where(p => p.Status == ProductStatus.Published && p.Id != productId)
            .Select(p => new
            {
                Product = p,
                Shared = db.ProductCategories.Count(l => l.ProductId == p.Id && categoryIds.Contains(l.CategoryId)),
            })
            .Where(x => x.Shared > 0);

        var totalCount = await related.CountAsync(ct);
        var rows = await related
            .OrderByDescending(x => x.Shared).ThenByDescending(x => x.Product.IsFeatured)
            .ThenBy(x => x.Product.Name).ThenBy(x => x.Product.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new
            {
                x.Product.Slug,
                x.Product.Name,
                x.Product.IsFeatured,
                MainImageKey = db.ProductImages.Where(i => i.ProductId == x.Product.Id).OrderBy(i => i.Position)
                    .Select(i => i.ThumbnailKey ?? i.StorageKey).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ListPublicProducts.Item(r.Slug, r.Name, r.MainImageKey is null ? null : storage.GetPublicUrl(r.MainImageKey), r.IsFeatured))
            .ToList();
        return TypedResults.Ok(new PagedResponse<ListPublicProducts.Item>(items, paging.Page, paging.PageSize, totalCount));
    }
}
