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
        Guid id, IFormFile? file, CatalogDbContext db, IImageStorage storage, ILoggerFactory loggers, CancellationToken ct)
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

        var thumbnail = ThumbnailGenerator.Generate(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (thumbnail is null)
        {
            return Invalid("The image could not be read. Upload a valid JPG, PNG or WebP file.");
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
        image.ThumbnailKey = await SaveThumbnailAsync(
            ThumbnailGenerator.KeyFor(product.TenantId, id, imageId), thumbnail, storage, loggers, ct);
        db.ProductImages.Add(image);
        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);

        var response = ProductImageResponse.From(image, storage);
        return TypedResults.Created($"/api/admin/products/{id}/images/{image.Id}", response);
    }

    /// <summary>
    /// The Thumbnail only makes listings lighter, so failing to store it does not fail the upload:
    /// the Image keeps no ThumbnailKey and responses fall back to the original.
    /// </summary>
    private static async Task<string?> SaveThumbnailAsync(
        string key, byte[] thumbnail, IImageStorage storage, ILoggerFactory loggers, CancellationToken ct)
    {
        try
        {
            using var stream = new MemoryStream(thumbnail);
            await storage.SaveAsync(key, stream, ThumbnailGenerator.ContentType, ct);
            return key;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            loggers.CreateLogger(typeof(UploadProductImage)).LogWarning(exception, "Could not store the Thumbnail {Key}", key);
            return null;
        }
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new ValidationErrors().Add("file", message).ToDictionary());
}
