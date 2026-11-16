using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Categories;

/// <summary>Deletes a leaf Category; its Products stay and simply leave it.</summary>
public static class DeleteCategory
{
    public static RouteGroupBuilder MapDeleteCategory(this RouteGroupBuilder admin)
    {
        admin.MapDelete("/categories/{id:guid}", Handle).WithName("DeleteCategory");
        return admin;
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, CatalogDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return TypedResults.NotFound();
        }

        if (await db.Categories.AnyAsync(c => c.ParentId == id, ct))
        {
            return TypedResults.Conflict(ProblemDetailsBody.Conflict("A Category with subcategories cannot be deleted."));
        }

        var links = await db.ProductCategories.Where(l => l.CategoryId == id).ToListAsync(ct);
        db.ProductCategories.RemoveRange(links);
        db.Categories.Remove(category);

        var siblings = await db.Categories.Where(c => c.ParentId == category.ParentId && c.Id != id).ToListAsync(ct);
        CategoryTree.Compact(siblings);

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
