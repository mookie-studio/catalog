using Catalog.Api.Data;
using Catalog.Api.Features.Admin.Profile;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

public static class GetPublicTenant
{
    public sealed record Response(
        string Name,
        string Slug,
        string? Description,
        string? ContactEmail,
        string? Phone,
        string? Address,
        string? WebsiteUrl,
        string? BrandColor,
        string? LogoUrl,
        bool InquiriesEnabled);

    public static RouteGroupBuilder MapGetPublicTenant(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("", Handle).WithName("GetPublicTenant");
        return publicGroup;
    }

    private static async Task<Ok<Response>> Handle(
        ICurrentTenant currentTenant, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var t = await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == currentTenant.TenantId, ct);
        return TypedResults.Ok(new Response(
            t.Name, t.Slug, t.Description, t.ContactEmail, t.Phone, t.Address, t.WebsiteUrl, t.BrandColor,
            TenantProfile.LogoUrl(t.LogoKey, storage), t.InquiriesEnabled));
    }
}
