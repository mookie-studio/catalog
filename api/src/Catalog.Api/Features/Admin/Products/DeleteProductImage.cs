using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Deletes an Image's file and row, then compacts the remaining positions.</summary>
public static class DeleteProductImage
{
    public static RouteGroupBuilder MapDeleteProductImage(this RouteGroupBuilder admin)
    {
        admin.MapDelete("/products/{id:guid}/images/{imageId:guid}", Handle).WithName("DeleteProductImage");
        return admin;
    }

    private static async Task<Results<NoContent, NotFound>> Handle(
        Guid id, Guid imageId, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        var images = await db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.Position).ToListAsync(ct);
        var image = images.SingleOrDefault(i => i.Id == imageId);
        if (product is null || image is null)
        {
            return TypedResults.NotFound();
        }

        db.ProductImages.Remove(image);
        images.Remove(image);
        for (var position = 0; position < images.Count; position++)
        {
            images[position].Position = position;
        }

        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync(image.StorageKey, ct);
        return TypedResults.NoContent();
    }
}
