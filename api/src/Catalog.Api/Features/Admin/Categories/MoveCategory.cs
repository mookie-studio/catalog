using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Categories;

/// <summary>Moves a Category under a new parent (null for root) at a 0-based position, keeping positions contiguous.</summary>
public static class MoveCategory
{
    public sealed record Request(Guid? ParentId, int Position);

    public static RouteGroupBuilder MapMoveCategory(this RouteGroupBuilder admin)
    {
        admin.MapPost("/categories/{id:guid}/move", Handle).WithName("MoveCategory");
        return admin;
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.ToListAsync(ct);
        var category = categories.SingleOrDefault(c => c.Id == id);
        if (category is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        if (request.ParentId is { } parentId)
        {
            if (categories.All(c => c.Id != parentId))
            {
                errors.Add("parentId", "The parent Category does not exist.");
            }
            else if (CategoryTree.SubtreeIds(categories, id).Contains(parentId))
            {
                errors.Add("parentId", "A Category cannot be moved under itself or one of its descendants.");
            }
        }

        if (request.Position < 0)
        {
            errors.Add("position", "The position must be 0 or greater.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var oldParentId = category.ParentId;
        var newSiblings = categories
            .Where(c => c.ParentId == request.ParentId && c.Id != id)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToList();
        newSiblings.Insert(Math.Min(request.Position, newSiblings.Count), category);
        category.ParentId = request.ParentId;
        for (var i = 0; i < newSiblings.Count; i++)
        {
            newSiblings[i].Position = i;
        }

        if (oldParentId != request.ParentId)
        {
            CategoryTree.Compact(categories.Where(c => c.ParentId == oldParentId));
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
