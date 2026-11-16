using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Updates the Product core fields in any status. Renaming never regenerates the Product slug.</summary>
public static class UpdateProduct
{
    public sealed record Request(string? Name, string? Description, string? Sku, string? Slug);

    public static RouteGroupBuilder MapUpdateProduct(this RouteGroupBuilder admin)
    {
        admin.MapPut("/products/{id:guid}", Handle).WithName("UpdateProduct");
        return admin;
    }

    private static async Task<Results<Ok<ProductResponse>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, Request request, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        ProductRules.ValidateName(request.Name, errors);
        ProductRules.ValidateSku(request.Sku, errors);
        ProductRules.ValidateDescription(request.Description, errors);
        ProductRules.ValidateSlug(request.Slug, errors);
        if (product.Status == ProductStatus.Published && string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("description", "A Published product requires a description.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var sku = request.Sku!.Trim();
        if (sku != product.Sku && await db.Products.AnyAsync(p => p.Sku == sku && p.Id != id, ct))
        {
            return Conflict("The SKU is already in use in this Tenant.", "sku");
        }

        if (request.Slug != product.Slug && await db.Products.AnyAsync(p => p.Slug == request.Slug && p.Id != id, ct))
        {
            return Conflict("The Product slug is already in use in this Tenant.", "slug");
        }

        product.Name = request.Name!.Trim();
        product.Description = ProductRules.NormalizeDescription(request.Description);
        product.Sku = sku;
        product.Slug = request.Slug!;
        product.UpdatedAt = Timestamps.UtcNow();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return Conflict("The SKU or Product slug is already in use in this Tenant.");
        }

        return TypedResults.Ok(await ProductResponse.LoadAsync(product, db, storage, ct));
    }

    private static Conflict<ProblemDetails> Conflict(string detail, string? field = null) =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict(detail, field));
}
