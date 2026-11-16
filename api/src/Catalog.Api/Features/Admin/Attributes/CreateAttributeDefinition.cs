using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Attributes;

public static class CreateAttributeDefinition
{
    public sealed record Request(string? Name, AttributeType? Type, bool IsFilterable, bool AllowsMultiple);

    public static RouteGroupBuilder MapCreateAttributeDefinition(this RouteGroupBuilder admin)
    {
        admin.MapPost("/attribute-definitions", Handle).WithName("CreateAttributeDefinition");
        return admin;
    }

    private static async Task<Results<Created<AttributeDefinitionResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        AttributeRules.ValidateName(request.Name, errors);
        if (request.Type is not { } type || !Enum.IsDefined(type))
        {
            errors.Add("type", "The type must be Text, Number, Boolean, Date or OptionList.");
        }
        else if (request.AllowsMultiple && type != AttributeType.OptionList)
        {
            errors.Add("allowsMultiple", "Only option-list Attribute Definitions can allow multiple options.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var name = request.Name!.Trim();
        var normalized = AttributeDefinition.Normalize(name);
        if (await db.AttributeDefinitions.AnyAsync(d => d.NormalizedName == normalized, ct))
        {
            return AttributeRules.NameInUse();
        }

        var definition = new AttributeDefinition
        {
            Name = name,
            Type = request.Type!.Value,
            IsFilterable = request.IsFilterable,
            AllowsMultiple = request.AllowsMultiple,
        };
        definition.Rename(name);
        db.AttributeDefinitions.Add(definition);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return AttributeRules.NameInUse();
        }

        return TypedResults.Created($"/api/admin/attribute-definitions/{definition.Id}", AttributeDefinitionResponse.From(definition));
    }
}
