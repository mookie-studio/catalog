using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Sets the order of a Product's Images; the first becomes the Main image.</summary>
public static class ReorderProductImages
{
    public sealed record Request(List<Guid>? ImageIds);

    public static RouteGroupBuilder MapReorderProductImages(this RouteGroupBuilder admin)
    {
        admin.MapPut("/products/{id:guid}/images/order", Handle).WithName("ReorderProductImages");
        return admin;
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var images = await db.ProductImages.Where(i => i.ProductId == id).ToListAsync(ct);
        var requested = request.ImageIds ?? [];
        if (requested.Count != images.Count || requested.Distinct().Count() != requested.Count
            || !requested.ToHashSet().SetEquals(images.Select(i => i.Id)))
        {
            return TypedResults.ValidationProblem(new ValidationErrors()
                .Add("imageIds", "The list must contain exactly the product's image ids, once each.").ToDictionary());
        }

        var byId = images.ToDictionary(i => i.Id);
        for (var position = 0; position < requested.Count; position++)
        {
            byId[requested[position]].Position = position;
        }

        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
