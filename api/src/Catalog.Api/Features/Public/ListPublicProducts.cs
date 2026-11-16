using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Published products of the Tenant, with full-text search (q), Category (including subcategories)
/// and Filterable attribute filters, paged.
/// </summary>
public static class ListPublicProducts
{
    public sealed record Item(string Slug, string Name, string? MainImageUrl);

    public static RouteGroupBuilder MapListPublicProducts(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/products", Handle).WithName("ListPublicProducts");
        return publicGroup;
    }

    private static async Task<Results<Ok<PagedResponse<Item>>, ValidationProblem>> Handle(
        int? page, int? pageSize, string? q, Guid? categoryId, HttpRequest request,
        CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        var filters = await AttributeFilters.ParseAsync(request.Query, db, errors, ct);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var paging = Paging.From(page, pageSize);
        var query = db.Products.Where(p => p.Status == ProductStatus.Published);
        query = AttributeFilters.Apply(query, filters);

        if (categoryId is { } category)
        {
            var subtree = (await PublicCategories.SubtreeAsync(category, db, ct)).ToList();
            query = query.Where(p => db.ProductCategories.Any(l => l.ProductId == p.Id && subtree.Contains(l.CategoryId)));
        }

        IOrderedQueryable<Product> ordered;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(p => p.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("spanish", search)));
            ordered = query
                .OrderByDescending(p => p.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("spanish", search)))
                .ThenBy(p => p.Name).ThenBy(p => p.Id);
        }
        else
        {
            ordered = query.OrderBy(p => p.Name).ThenBy(p => p.Id);
        }

        var totalCount = await query.CountAsync(ct);
        var rows = await ordered
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(p => new
            {
                p.Slug,
                p.Name,
                MainImageKey = db.ProductImages.Where(i => i.ProductId == p.Id).OrderBy(i => i.Position)
                    .Select(i => i.StorageKey).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new Item(r.Slug, r.Name, r.MainImageKey is null ? null : storage.GetPublicUrl(r.MainImageKey)))
            .ToList();
        return TypedResults.Ok(new PagedResponse<Item>(items, paging.Page, paging.PageSize, totalCount));
    }
}
