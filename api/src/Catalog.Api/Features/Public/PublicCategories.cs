using Catalog.Api.Data;
using Catalog.Api.Features.Admin.Categories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>The Category tree for Visitors: Categories without Published products in their subtree are hidden.</summary>
public static class PublicCategories
{
    public static RouteGroupBuilder MapListPublicCategories(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/categories", Handle).WithName("ListPublicCategories");
        return publicGroup;
    }

    private static async Task<Ok<List<CategoryNode>>> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync(ct);
        var withPublished = await db.ProductCategories
            .Where(l => db.Products.Any(p => p.Id == l.ProductId && p.Status == ProductStatus.Published))
            .Select(l => l.CategoryId)
            .Distinct()
            .ToListAsync(ct);

        var visible = new HashSet<Guid>();
        var byId = categories.ToDictionary(c => c.Id);
        foreach (var id in withPublished)
        {
            // A Category with a Published product makes it and all its ancestors visible.
            for (Guid? current = id; current is { } c && byId.TryGetValue(c, out var category) && visible.Add(c);)
            {
                current = category.ParentId;
            }
        }

        return TypedResults.Ok(CategoryTree.Build(categories.Where(c => visible.Contains(c.Id)).ToList()));
    }

    /// <summary>The Category and all its descendants, for "products in this Category".</summary>
    public static async Task<HashSet<Guid>> SubtreeAsync(Guid categoryId, CatalogDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync(ct);
        return categories.Any(c => c.Id == categoryId) ? CategoryTree.SubtreeIds(categories, categoryId) : [];
    }
}
