using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Categories;

public static class RenameCategory
{
    public sealed record Request(string? Name);

    public static RouteGroupBuilder MapRenameCategory(this RouteGroupBuilder admin)
    {
        admin.MapPut("/categories/{id:guid}", Handle).WithName("RenameCategory");
        return admin;
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        CategoryTree.ValidateName(request.Name, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        category.Name = request.Name!.Trim();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
