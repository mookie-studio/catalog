using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Attributes;

/// <summary>
/// Deletes an Attribute Definition or one of its Options. When products use it, the Admin must confirm,
/// and the values are then removed from those products (ADR 0002).
/// </summary>
public static class DeleteAttributeDefinition
{
    public static RouteGroupBuilder MapDeleteAttributeDefinition(this RouteGroupBuilder admin)
    {
        admin.MapDelete("/attribute-definitions/{id:guid}", DeleteDefinition).WithName("DeleteAttributeDefinition");
        admin.MapDelete("/attribute-definitions/{id:guid}/options/{optionId:guid}", DeleteOption).WithName("DeleteAttributeOption");
        return admin;
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> DeleteDefinition(
        Guid id, bool? confirm, CatalogDbContext db, CancellationToken ct)
    {
        var definition = await db.AttributeDefinitions.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (definition is null)
        {
            return TypedResults.NotFound();
        }

        var conflict = await RemoveUsages(id, null, confirm == true, db, ct);
        if (conflict is not null)
        {
            return conflict;
        }

        db.AttributeDefinitions.Remove(definition);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> DeleteOption(
        Guid id, Guid optionId, bool? confirm, CatalogDbContext db, CancellationToken ct)
    {
        var option = await db.AttributeOptions.SingleOrDefaultAsync(o => o.Id == optionId && o.AttributeDefinitionId == id, ct);
        if (option is null)
        {
            return TypedResults.NotFound();
        }

        var conflict = await RemoveUsages(id, optionId, confirm == true, db, ct);
        if (conflict is not null)
        {
            return conflict;
        }

        db.AttributeOptions.Remove(option);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>Returns a 409 with usageCount when used and not confirmed; otherwise strips the values (not yet saved).</summary>
    private static async Task<Conflict<ProblemDetails>?> RemoveUsages(
        Guid definitionId, Guid? optionId, bool confirmed, CatalogDbContext db, CancellationToken ct)
    {
        var key = definitionId.ToString();
        var candidates = await db.Products.Where(p => EF.Functions.JsonExists(p.Attributes, key)).ToListAsync(ct);
        var using_ = candidates.Where(p => ProductAttributes.Uses(p.Attributes, definitionId, optionId)).ToList();

        if (using_.Count == 0)
        {
            return null;
        }

        if (!confirmed)
        {
            var problem = ProblemDetailsBody.Conflict(
                $"{using_.Count} product(s) use it. Send confirm=true to remove those values and delete it.");
            problem.Extensions["usageCount"] = using_.Count;
            return TypedResults.Conflict(problem);
        }

        var now = Timestamps.UtcNow();
        foreach (var product in using_)
        {
            product.Attributes = ProductAttributes.Remove(product.Attributes, definitionId, optionId);
            product.UpdatedAt = now;
        }

        return null;
    }
}
