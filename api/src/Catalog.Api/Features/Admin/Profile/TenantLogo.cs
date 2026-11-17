using Catalog.Api.Data;
using Catalog.Api.Features.Admin.Products;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Profile;

/// <summary>Uploads (multipart field "file") or removes the current Tenant's Logo.</summary>
public static class TenantLogo
{
    public const long MaxBytes = 1024 * 1024;

    public sealed record Response(string LogoUrl);

    public static RouteGroupBuilder MapTenantLogo(this RouteGroupBuilder admin)
    {
        admin.MapPut("/tenant/logo", Upload)
            .WithName("UploadTenantLogo")
            .DisableAntiforgery(); // Bearer-token API: no cookies, so no CSRF exposure.
        admin.MapDelete("/tenant/logo", Remove).WithName("RemoveTenantLogo");
        return admin;
    }

    private static async Task<Results<Ok<Response>, ValidationProblem, NotFound>> Upload(
        IFormFile? file, ICurrentTenant currentTenant, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == currentTenant.TenantId, ct);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        if (file is null || file.Length == 0)
        {
            return Invalid("An image file is required in the 'file' field.");
        }

        if (file.Length > MaxBytes)
        {
            return Invalid("The logo must be at most 1 MB.");
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var format = ProductImages.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));
        if (format is null)
        {
            return Invalid("Only JPG, PNG or WebP images are allowed.");
        }

        var previousKey = tenant.LogoKey;
        tenant.LogoKey = $"{tenant.Id}/logo/{Guid.CreateVersion7()}.{format.Extension}";
        buffer.Position = 0;
        await storage.SaveAsync(tenant.LogoKey, buffer, format.ContentType, ct);
        await db.SaveChangesAsync(ct);

        if (previousKey is not null)
        {
            await storage.DeleteAsync(previousKey, ct);
        }

        return TypedResults.Ok(new Response(storage.GetPublicUrl(tenant.LogoKey)));
    }

    private static async Task<Results<NoContent, NotFound>> Remove(
        ICurrentTenant currentTenant, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == currentTenant.TenantId, ct);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        if (tenant.LogoKey is { } key)
        {
            tenant.LogoKey = null;
            await db.SaveChangesAsync(ct);
            await storage.DeleteAsync(key, ct);
        }

        return TypedResults.NoContent();
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new ValidationErrors().Add("file", message).ToDictionary());
}
