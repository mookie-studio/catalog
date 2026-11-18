using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>A Bulk action: one change applied to several products (see GLOSSARY.md).</summary>
public enum BulkAction
{
    Publish,
    Archive,
    MoveToDraft,
    Feature,
    Unfeature,
    AddToCategory,
    RemoveFromCategory,
}

/// <summary>
/// Applies a Bulk action to up to 100 products. Each product succeeds or fails on its own with the same rules as the
/// single-product change; successful changes are saved together.
/// </summary>
public static class BulkProductAction
{
    public const int MaxProducts = 100;

    public sealed record Request(List<Guid>? ProductIds, BulkAction? Action, Guid? CategoryId);

    public sealed record Failure(Guid ProductId, IDictionary<string, string[]> Errors);

    public sealed record Response(IReadOnlyList<Guid> Succeeded, IReadOnlyList<Failure> Failed);

    public static RouteGroupBuilder MapBulkProductAction(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products/bulk", Handle).WithName("BulkProductAction");
        return admin;
    }

    private static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        var ids = request.ProductIds ?? [];
        if (ids.Count is 0 or > MaxProducts)
        {
            errors.Add("productIds", $"Select between 1 and {MaxProducts} products.");
        }
        else if (ids.Distinct().Count() != ids.Count)
        {
            errors.Add("productIds", "Each product can be listed only once.");
        }

        if (request.Action is not { } action || !Enum.IsDefined(action))
        {
            errors.Add("action", "The action must be Publish, Archive, MoveToDraft, Feature, Unfeature, AddToCategory or RemoveFromCategory.");
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var isCategoryAction = action is BulkAction.AddToCategory or BulkAction.RemoveFromCategory;
        if (isCategoryAction && (request.CategoryId is not { } categoryId || !await db.Categories.AnyAsync(c => c.Id == categoryId, ct)))
        {
            errors.Add("categoryId", "Choose an existing Category.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var links = isCategoryAction
            ? await db.ProductCategories
                .Where(l => l.CategoryId == request.CategoryId && ids.Contains(l.ProductId))
                .ToDictionaryAsync(l => l.ProductId, ct)
            : [];

        var now = Timestamps.UtcNow();
        var succeeded = new List<Guid>();
        var failed = new List<Failure>();
        foreach (var id in ids)
        {
            if (!products.TryGetValue(id, out var product))
            {
                failed.Add(new Failure(id, new ValidationErrors().Add("productId", "Product not found.").ToDictionary()));
                continue;
            }

            var productErrors = new ValidationErrors();
            var changed = action switch
            {
                BulkAction.Publish => Publish(product, productErrors, now),
                BulkAction.Archive => SetStatus(product, ProductStatus.Archived),
                BulkAction.MoveToDraft => SetStatus(product, ProductStatus.Draft),
                BulkAction.Feature => SetFeatured(product, true),
                BulkAction.Unfeature => SetFeatured(product, false),
                BulkAction.AddToCategory => AddToCategory(db, product, request.CategoryId!.Value, links),
                BulkAction.RemoveFromCategory => RemoveFromCategory(db, product, links),
                _ => false,
            };

            if (productErrors.HasErrors)
            {
                failed.Add(new Failure(id, productErrors.ToDictionary()));
                continue;
            }

            if (changed)
            {
                product.UpdatedAt = now;
            }

            succeeded.Add(id);
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new Response(succeeded, failed));
    }

    private static bool Publish(Product product, ValidationErrors errors, DateTimeOffset now)
    {
        ProductRules.ValidatePublishable(product.Name, product.Sku, product.Description, errors);
        if (errors.HasErrors || !SetStatus(product, ProductStatus.Published))
        {
            return false;
        }

        product.PublishedAt = now;
        return true;
    }

    private static bool SetStatus(Product product, ProductStatus status)
    {
        if (product.Status == status)
        {
            return false;
        }

        product.Status = status;
        return true;
    }

    private static bool SetFeatured(Product product, bool featured)
    {
        if (product.IsFeatured == featured)
        {
            return false;
        }

        product.IsFeatured = featured;
        return true;
    }

    private static bool AddToCategory(
        CatalogDbContext db, Product product, Guid categoryId, Dictionary<Guid, ProductCategory> links)
    {
        if (links.ContainsKey(product.Id))
        {
            return false;
        }

        db.ProductCategories.Add(new ProductCategory { ProductId = product.Id, CategoryId = categoryId });
        return true;
    }

    private static bool RemoveFromCategory(CatalogDbContext db, Product product, Dictionary<Guid, ProductCategory> links)
    {
        if (!links.TryGetValue(product.Id, out var link))
        {
            return false;
        }

        db.ProductCategories.Remove(link);
        return true;
    }
}
