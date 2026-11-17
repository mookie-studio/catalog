using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Published products of the Tenant, with full-text search (q), Category (including subcategories),
/// Filterable attribute filters and Featured products, paged and sorted.
/// sort: relevance (default with q), featured (default otherwise), name, newest. Ties break by name, then id.
/// </summary>
public static class ListPublicProducts
{
    public sealed record Item(string Slug, string Name, string? MainImageUrl, bool IsFeatured);

    public static readonly string[] Sorts = ["relevance", "featured", "name", "newest"];

    public static RouteGroupBuilder MapListPublicProducts(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/products", Handle).WithName("ListPublicProducts");
        return publicGroup;
    }

    private static async Task<Results<Ok<PagedResponse<Item>>, ValidationProblem>> Handle(
        int? page, int? pageSize, string? q, Guid? categoryId, string? sort, bool? featured, HttpRequest request,
        CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        var filters = await AttributeFilters.ParseAsync(request.Query, db, errors, ct);
        if (sort is not null && !Sorts.Contains(sort))
        {
            errors.Add("sort", "The sort must be relevance, featured, name or newest.");
        }

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

        if (featured == true)
        {
            query = query.Where(p => p.IsFeatured);
        }

        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        if (search is not null)
        {
            query = query.Where(p => p.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("spanish", search)));
        }

        var effectiveSort = sort ?? (search is null ? "featured" : "relevance");
        if (effectiveSort == "relevance" && search is null)
        {
            effectiveSort = "featured";
        }

        var ordered = effectiveSort switch
        {
            "relevance" => query
                .OrderByDescending(p => p.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("spanish", search!)))
                .ThenBy(p => p.Name).ThenBy(p => p.Id),
            "name" => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            "newest" => query.OrderByDescending(p => p.PublishedAt).ThenBy(p => p.Name).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name).ThenBy(p => p.Id),
        };

        var totalCount = await query.CountAsync(ct);
        var rows = await ordered
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(p => new
            {
                p.Slug,
                p.Name,
                p.IsFeatured,
                MainImageKey = db.ProductImages.Where(i => i.ProductId == p.Id).OrderBy(i => i.Position)
                    .Select(i => i.StorageKey).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new Item(r.Slug, r.Name, r.MainImageKey is null ? null : storage.GetPublicUrl(r.MainImageKey), r.IsFeatured))
            .ToList();
        return TypedResults.Ok(new PagedResponse<Item>(items, paging.Page, paging.PageSize, totalCount));
    }
}
