using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Categories;

public static class ListCategories
{
    public static RouteGroupBuilder MapListCategories(this RouteGroupBuilder admin)
    {
        admin.MapGet("/categories", Handle).WithName("ListCategories");
        return admin;
    }

    private static async Task<Ok<List<CategoryNode>>> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync(ct);
        return TypedResults.Ok(CategoryTree.Build(categories));
    }
}
