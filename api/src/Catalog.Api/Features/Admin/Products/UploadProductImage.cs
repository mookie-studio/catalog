using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Uploads an Image (multipart field "file") and appends it last.</summary>
public static class UploadProductImage
{
    public static RouteGroupBuilder MapUploadProductImage(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products/{id:guid}/images", Handle)
            .WithName("UploadProductImage")
            .DisableAntiforgery(); // Bearer-token API: no cookies, so no CSRF exposure.
        return admin;
    }

    private static async Task<Results<Created<ProductImageResponse>, ValidationProblem, NotFound>> Handle(
        Guid id, IFormFile? file, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        if (file is null || file.Length == 0)
        {
            return Invalid("An image file is required in the 'file' field.");
        }

        if (file.Length > ProductImages.MaxBytes)
        {
            return Invalid("The image must be at most 2 MB.");
        }

        var count = await db.ProductImages.CountAsync(i => i.ProductId == id, ct);
        if (count >= ProductImages.MaxPerProduct)
        {
            return Invalid($"A product can have at most {ProductImages.MaxPerProduct} images.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var format = ProductImages.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));
        if (format is null)
        {
            return Invalid("Only JPG, PNG or WebP images are allowed.");
        }

        var imageId = Guid.CreateVersion7();
        var image = new ProductImage
        {
            Id = imageId,
            ProductId = id,
            ContentType = format.ContentType,
            StorageKey = $"{product.TenantId}/{id}/{imageId}.{format.Extension}",
            Position = count,
        };

        buffer.Position = 0;
        await storage.SaveAsync(image.StorageKey, buffer, format.ContentType, ct);
        db.ProductImages.Add(image);
        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);

        var response = new ProductImageResponse(image.Id, storage.GetPublicUrl(image.StorageKey), image.Position);
        return TypedResults.Created($"/api/admin/products/{id}/images/{image.Id}", response);
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new ValidationErrors().Add("file", message).ToDictionary());
}
