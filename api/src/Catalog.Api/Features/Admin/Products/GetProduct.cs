using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

public static class GetProduct
{
    public static RouteGroupBuilder MapGetProduct(this RouteGroupBuilder admin)
    {
        admin.MapGet("/products/{id:guid}", Handle).WithName("GetProduct");
        return admin;
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> Handle(Guid id, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(ProductResponse.From(product));
    }
}
