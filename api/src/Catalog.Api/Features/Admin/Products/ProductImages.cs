using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

public sealed record ProductImageResponse(Guid Id, string Url, string ThumbnailUrl, int Position)
{
    public static ProductImageResponse From(ProductImage image, IImageStorage storage) =>
        new(image.Id, storage.GetPublicUrl(image.StorageKey), ProductImages.ThumbnailUrl(image, storage), image.Position);
}

/// <summary>Image rules (ADR 0005): JPG, PNG or WebP by magic bytes, at most 2 MB and 5 per Product.</summary>
public static class ProductImages
{
    public const long MaxBytes = 2 * 1024 * 1024;
    public const int MaxPerProduct = 5;

    public sealed record Format(string ContentType, string Extension);

    /// <summary>Detects the format from the file's first bytes; the file name and declared type are ignored.</summary>
    public static Format? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new Format("image/jpeg", "jpg");
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return new Format("image/png", "png");
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            return new Format("image/webp", "webp");
        }

        return null;
    }

    /// <summary>The Thumbnail's URL, or the original's when the image has no Thumbnail.</summary>
    public static string ThumbnailUrl(ProductImage image, IImageStorage storage) =>
        storage.GetPublicUrl(image.ThumbnailKey ?? image.StorageKey);

    public static async Task<List<ProductImageResponse>> ListAsync(
        Guid productId, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var images = await db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Position)
            .ToListAsync(ct);
        return images.Select(i => ProductImageResponse.From(i, storage)).ToList();
    }
}
