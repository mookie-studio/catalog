using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Replaces a Product's Categories (zero, one or many).</summary>
public static class SetProductCategories
{
    public sealed record Request(List<Guid>? CategoryIds);

    public static RouteGroupBuilder MapSetProductCategories(this RouteGroupBuilder admin)
    {
        admin.MapPut("/products/{id:guid}/categories", Handle).WithName("SetProductCategories");
        return admin;
    }

    private static async Task<Results<Ok<ProductResponse>, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var requested = (request.CategoryIds ?? []).Distinct().ToList();
        var known = await db.Categories.Where(c => requested.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        var unknown = requested.Except(known).ToList();
        if (unknown.Count > 0)
        {
            return TypedResults.ValidationProblem(new ValidationErrors()
                .Add("categoryIds", $"Unknown Category ids: {string.Join(", ", unknown)}.").ToDictionary());
        }

        var existing = await db.ProductCategories.Where(l => l.ProductId == id).ToListAsync(ct);
        db.ProductCategories.RemoveRange(existing.Where(l => !requested.Contains(l.CategoryId)));
        foreach (var categoryId in requested.Where(c => existing.All(l => l.CategoryId != c)))
        {
            db.ProductCategories.Add(new ProductCategory { ProductId = id, CategoryId = categoryId });
        }

        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ProductResponse.LoadAsync(product, db, ct));
    }
}
