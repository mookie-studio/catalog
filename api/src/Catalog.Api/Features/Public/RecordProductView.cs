using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Counts one Product view of a Published product (ADR 0012): one atomic upsert on the (product, UTC day) counter.
/// Nothing about the Visitor is stored.
/// </summary>
public static class RecordProductView
{
    public const string RateLimitPolicy = "product-views";

    public static RouteGroupBuilder MapRecordProductView(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapPost("/products/{productSlug}/views", Handle)
            .WithName("RecordProductView")
            .RequireRateLimiting(RateLimitPolicy);
        return publicGroup;
    }

    private static async Task<Results<Accepted, NotFound>> Handle(
        string productSlug, ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        // An Old Product slug resolves to its product (ADR 0009).
        var productId = await db.Products
            .Where(p => p.Status == ProductStatus.Published)
            .Where(p => p.Slug == productSlug || db.OldProductSlugs.Any(o => o.ProductId == p.Id && o.Slug == productSlug))
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (productId is null)
        {
            return TypedResults.NotFound();
        }

        var tenantId = currentTenant.TenantId!.Value;
        var day = DateOnly.FromDateTime(Timestamps.UtcNow().UtcDateTime);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductViewCounts" ("Id", "TenantId", "ProductId", "Day", "Count")
            VALUES ({Guid.CreateVersion7()}, {tenantId}, {productId.Value}, {day}, 1)
            ON CONFLICT ("ProductId", "Day") DO UPDATE SET "Count" = "ProductViewCounts"."Count" + 1
            """, ct);

        return TypedResults.Accepted((string?)null);
    }
}
