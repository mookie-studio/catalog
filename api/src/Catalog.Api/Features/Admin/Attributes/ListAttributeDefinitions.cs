using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Attributes;

public static class ListAttributeDefinitions
{
    public static RouteGroupBuilder MapListAttributeDefinitions(this RouteGroupBuilder admin)
    {
        admin.MapGet("/attribute-definitions", Handle).WithName("ListAttributeDefinitions");
        return admin;
    }

    private static async Task<Ok<List<AttributeDefinitionResponse>>> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var definitions = await db.AttributeDefinitions
            .Include(d => d.Options)
            .OrderBy(d => d.NormalizedName)
            .AsNoTracking()
            .ToListAsync(ct);

        return TypedResults.Ok(definitions.Select(AttributeDefinitionResponse.From).ToList());
    }
}
