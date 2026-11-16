using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Moves a Product to any Product status. Publishing requires name, SKU and description.</summary>
public static class ChangeProductStatus
{
    public sealed record Request(ProductStatus? Status);

    public static RouteGroupBuilder MapChangeProductStatus(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products/{id:guid}/status", Handle).WithName("ChangeProductStatus");
        return admin;
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        if (request.Status is not { } status || !Enum.IsDefined(status))
        {
            return TypedResults.ValidationProblem(errors.Add("status", "The status must be Draft, Published or Archived.").ToDictionary());
        }

        if (status == ProductStatus.Published)
        {
            ProductRules.ValidatePublishable(product.Name, product.Sku, product.Description, errors);
            if (errors.HasErrors)
            {
                return TypedResults.ValidationProblem(errors.ToDictionary());
            }
        }

        if (product.Status != status)
        {
            product.Status = status;
            product.UpdatedAt = Timestamps.UtcNow();
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.NoContent();
    }
}
