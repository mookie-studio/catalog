using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Marks or unmarks a Product as a Featured product.</summary>
public static class SetProductFeatured
{
    public sealed record Request(bool? Featured);

    public static RouteGroupBuilder MapSetProductFeatured(this RouteGroupBuilder admin)
    {
        admin.MapPut("/products/{id:guid}/featured", Handle).WithName("SetProductFeatured");
        return admin;
    }

    private static async Task<Results<Ok<ProductResponse>, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Featured is not { } featured)
        {
            return TypedResults.ValidationProblem(new ValidationErrors().Add("featured", "Featured is required.").ToDictionary());
        }

        if (product.IsFeatured != featured)
        {
            product.IsFeatured = featured;
            product.UpdatedAt = Timestamps.UtcNow();
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(await ProductResponse.LoadAsync(product, db, storage, ct));
    }
}
