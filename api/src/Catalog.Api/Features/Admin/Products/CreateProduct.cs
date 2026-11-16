using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Creates a Draft Product; a Draft only requires name and SKU.</summary>
public static class CreateProduct
{
    public sealed record Request(string? Name, string? Sku, string? Description);

    public static RouteGroupBuilder MapCreateProduct(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products", Handle).WithName("CreateProduct");
        return admin;
    }

    private static async Task<Results<Created<ProductResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        ProductRules.ValidateName(request.Name, errors);
        ProductRules.ValidateSku(request.Sku, errors);
        ProductRules.ValidateDescription(request.Description, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var name = request.Name!.Trim();
        var sku = request.Sku!.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return SkuInUse();
        }

        var product = new Product
        {
            Name = name,
            Sku = sku,
            Description = ProductRules.NormalizeDescription(request.Description),
            Slug = await ProductRules.GenerateUniqueSlugAsync(name, db, ct),
        };
        db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return TypedResults.Conflict(ProblemDetailsBody.Conflict("The SKU or Product slug is already in use."));
        }

        return TypedResults.Created($"/api/admin/products/{product.Id}", ProductResponse.From(product, [], [], []));
    }

    private static Conflict<ProblemDetails> SkuInUse() =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict("The SKU is already in use in this Tenant.", "sku"));
}
