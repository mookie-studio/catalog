using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin;

/// <summary>The Dashboard: counts of the current Tenant's products, Open Inquiries and quality warnings.</summary>
public static class GetDashboard
{
    /// <param name="FeaturedCount">Products that are both Published and Featured.</param>
    /// <param name="HasCategories">Whether the Tenant has at least one Category.</param>
    public sealed record Response(
        int DraftCount,
        int PublishedCount,
        int ArchivedCount,
        int FeaturedCount,
        int OpenInquiryCount,
        int PublishedWithoutImagesCount,
        int PublishedWithoutCategoriesCount,
        bool HasCategories);

    public static RouteGroupBuilder MapGetDashboard(this RouteGroupBuilder admin)
    {
        admin.MapGet("/dashboard", Handle).WithName("GetDashboard");
        return admin;
    }

    private static async Task<Ok<Response>> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var byStatus = await db.Products
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);
        var published = db.Products.Where(p => p.Status == ProductStatus.Published);

        return TypedResults.Ok(new Response(
            byStatus.GetValueOrDefault(ProductStatus.Draft),
            byStatus.GetValueOrDefault(ProductStatus.Published),
            byStatus.GetValueOrDefault(ProductStatus.Archived),
            await published.CountAsync(p => p.IsFeatured, ct),
            await db.Inquiries.CountAsync(i => i.Status == InquiryStatus.Open, ct),
            await published.CountAsync(p => !db.ProductImages.Any(i => i.ProductId == p.Id), ct),
            await published.CountAsync(p => !db.ProductCategories.Any(l => l.ProductId == p.Id), ct),
            await db.Categories.AnyAsync(ct)));
    }
}
