using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Categories;

/// <summary>Creates a Category as the last sibling under its parent (or at the root).</summary>
public static class CreateCategory
{
    public sealed record Request(string? Name, Guid? ParentId);

    public static RouteGroupBuilder MapCreateCategory(this RouteGroupBuilder admin)
    {
        admin.MapPost("/categories", Handle).WithName("CreateCategory");
        return admin;
    }

    private static async Task<Results<Created<CategoryNode>, ValidationProblem>> Handle(
        Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        CategoryTree.ValidateName(request.Name, errors);
        if (request.ParentId is { } parentId && !await db.Categories.AnyAsync(c => c.Id == parentId, ct))
        {
            errors.Add("parentId", "The parent Category does not exist.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var siblingCount = await db.Categories.CountAsync(c => c.ParentId == request.ParentId, ct);
        var category = new Category { Name = request.Name!.Trim(), ParentId = request.ParentId, Position = siblingCount };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/admin/categories/{category.Id}", new CategoryNode(category.Id, category.Name, []));
    }
}
