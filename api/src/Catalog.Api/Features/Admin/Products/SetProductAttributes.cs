using System.Text.Json;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Replaces all of a Product's Attributes. All Attributes are optional.</summary>
public static class SetProductAttributes
{
    public sealed record Request(Dictionary<string, JsonElement>? Values);

    public static RouteGroupBuilder MapSetProductAttributes(this RouteGroupBuilder admin)
    {
        admin.MapPut("/products/{id:guid}/attributes", Handle).WithName("SetProductAttributes");
        return admin;
    }

    private static async Task<Results<Ok<ProductResponse>, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var definitions = await db.AttributeDefinitions.Include(d => d.Options).AsNoTracking().ToDictionaryAsync(d => d.Id, ct);
        var errors = new ValidationErrors();
        var attributes = ProductAttributes.Validate(request.Values ?? [], definitions, errors);
        if (attributes is null)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        product.Attributes = attributes;
        product.UpdatedAt = Timestamps.UtcNow();
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(await ProductResponse.LoadAsync(product, db, ct));
    }
}
