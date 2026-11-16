using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Attributes;

/// <summary>Appends an Option to an option-list Attribute Definition.</summary>
public static class AddAttributeOption
{
    public sealed record Request(string? Value);

    public static RouteGroupBuilder MapAddAttributeOption(this RouteGroupBuilder admin)
    {
        admin.MapPost("/attribute-definitions/{id:guid}/options", Handle).WithName("AddAttributeOption");
        return admin;
    }

    private static async Task<Results<Created<AttributeOptionResponse>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var definition = await db.AttributeDefinitions.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (definition is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new ValidationErrors();
        if (definition.Type != AttributeType.OptionList)
        {
            errors.Add("value", "Options can only be added to option-list Attribute Definitions.");
        }
        else if (string.IsNullOrWhiteSpace(request.Value))
        {
            errors.Add("value", "The value is required.");
        }
        else if (request.Value.Trim().Length > AttributeOption.ValueMaxLength)
        {
            errors.Add("value", $"The value must be at most {AttributeOption.ValueMaxLength} characters.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var value = request.Value!.Trim();
        var normalized = value.ToLowerInvariant();
        var existing = await db.AttributeOptions.Where(o => o.AttributeDefinitionId == id).ToListAsync(ct);
        if (existing.Any(o => o.NormalizedValue == normalized))
        {
            return ValueInUse();
        }

        var option = new AttributeOption
        {
            AttributeDefinitionId = id,
            Value = value,
            NormalizedValue = normalized,
            Position = existing.Count == 0 ? 0 : existing.Max(o => o.Position) + 1,
        };
        db.AttributeOptions.Add(option);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return ValueInUse();
        }

        return TypedResults.Created(
            $"/api/admin/attribute-definitions/{id}/options/{option.Id}",
            new AttributeOptionResponse(option.Id, option.Value, option.Position));
    }

    private static Conflict<ProblemDetails> ValueInUse() =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict("This Option already exists in the Attribute Definition.", "value"));
}
