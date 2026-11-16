using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Attributes;

/// <summary>Renames an Attribute Definition or toggles filterable. Its type and allowsMultiple never change.</summary>
public static class UpdateAttributeDefinition
{
    public sealed record Request(string? Name, bool IsFilterable);

    public static RouteGroupBuilder MapUpdateAttributeDefinition(this RouteGroupBuilder admin)
    {
        admin.MapPut("/attribute-definitions/{id:guid}", Handle).WithName("UpdateAttributeDefinition");
        return admin;
    }

    private static async Task<Results<Ok<AttributeDefinitionResponse>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var definition = await db.AttributeDefinitions.Include(d => d.Options).SingleOrDefaultAsync(d => d.Id == id, ct);
        if (definition is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        AttributeRules.ValidateName(request.Name, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var name = request.Name!.Trim();
        var normalized = AttributeDefinition.Normalize(name);
        if (await db.AttributeDefinitions.AnyAsync(d => d.NormalizedName == normalized && d.Id != id, ct))
        {
            return AttributeRules.NameInUse();
        }

        definition.Rename(name);
        definition.IsFilterable = request.IsFilterable;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return AttributeRules.NameInUse();
        }

        return TypedResults.Ok(AttributeDefinitionResponse.From(definition));
    }
}
