using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>
/// Duplicates a Product as a new Draft: "(copy)" name, unique "-COPY" SKU, new Product slug,
/// same description, Attributes, Categories and Images (with their Thumbnails). It is never Featured.
/// </summary>
public static class DuplicateProduct
{
    public const string NameSuffix = " (copy)";
    public const string SkuSuffix = "-COPY";

    public static RouteGroupBuilder MapDuplicateProduct(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products/{id:guid}/duplicate", Handle).WithName("DuplicateProduct");
        return admin;
    }

    private static async Task<Results<Created<ProductResponse>, NotFound>> Handle(
        Guid id, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var source = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        if (source is null)
        {
            return TypedResults.NotFound();
        }

        var name = Cut(source.Name, Product.NameMaxLength - NameSuffix.Length) + NameSuffix;
        var copy = new Product
        {
            Name = name,
            Sku = await UniqueSkuAsync(source.Sku, db, ct),
            Slug = await ProductRules.GenerateUniqueSlugAsync(name, db, ct),
            Description = source.Description,
            Attributes = source.Attributes,
            Status = ProductStatus.Draft,
            IsFeatured = false,
        };
        db.Products.Add(copy);

        var categoryIds = await db.ProductCategories.Where(l => l.ProductId == id).Select(l => l.CategoryId).ToListAsync(ct);
        db.ProductCategories.AddRange(categoryIds.Select(categoryId => new ProductCategory { ProductId = copy.Id, CategoryId = categoryId }));

        var copiedKeys = new List<string>();
        try
        {
            var images = await db.ProductImages.AsNoTracking().Where(i => i.ProductId == id).OrderBy(i => i.Position).ToListAsync(ct);
            foreach (var image in images)
            {
                db.ProductImages.Add(await CopyImageAsync(image, source.TenantId, copy.Id, storage, copiedKeys, ct));
            }

            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Leave no orphaned objects behind when the copy cannot be completed.
            foreach (var key in copiedKeys)
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }

            throw;
        }

        return TypedResults.Created($"/api/admin/products/{copy.Id}", await ProductResponse.LoadAsync(copy, db, storage, ct));
    }

    private static async Task<ProductImage> CopyImageAsync(
        ProductImage image, Guid tenantId, Guid productId, IImageStorage storage, List<string> copiedKeys, CancellationToken ct)
    {
        var imageId = Guid.CreateVersion7();
        var copy = new ProductImage
        {
            Id = imageId,
            ProductId = productId,
            ContentType = image.ContentType,
            StorageKey = $"{tenantId}/{productId}/{imageId}{Path.GetExtension(image.StorageKey)}",
            Position = image.Position,
        };
        await storage.CopyAsync(image.StorageKey, copy.StorageKey, ct);
        copiedKeys.Add(copy.StorageKey);

        if (image.ThumbnailKey is not null)
        {
            var thumbnailKey = ThumbnailGenerator.KeyFor(tenantId, productId, imageId);
            await storage.CopyAsync(image.ThumbnailKey, thumbnailKey, ct);
            copiedKeys.Add(thumbnailKey);
            copy.ThumbnailKey = thumbnailKey;
        }

        return copy;
    }

    /// <summary>"{sku}-COPY", then "-COPY-2", "-COPY-3", … until unused; the original is cut so the result fits.</summary>
    private static async Task<string> UniqueSkuAsync(string sku, CatalogDbContext db, CancellationToken ct)
    {
        for (var n = 1; ; n++)
        {
            var suffix = n == 1 ? SkuSuffix : $"{SkuSuffix}-{n}";
            var candidate = Cut(sku, Product.SkuMaxLength - suffix.Length) + suffix;
            if (!await db.Products.AnyAsync(p => p.Sku == candidate, ct))
            {
                return candidate;
            }
        }
    }

    private static string Cut(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength].TrimEnd();
}
