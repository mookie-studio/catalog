using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

public static class ListPublicFilters
{
    public sealed record Option(Guid Id, string Value);

    public sealed record Filter(Guid DefinitionId, string Name, AttributeType Type, bool AllowsMultiple, IReadOnlyList<Option>? Options);

    public static RouteGroupBuilder MapListPublicFilters(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/filters", Handle).WithName("ListPublicFilters");
        return publicGroup;
    }

    private static async Task<Ok<List<Filter>>> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var definitions = await db.AttributeDefinitions
            .Where(d => d.IsFilterable)
            .Include(d => d.Options)
            .OrderBy(d => d.NormalizedName)
            .AsNoTracking()
            .ToListAsync(ct);

        return TypedResults.Ok(definitions.Select(d => new Filter(
            d.Id, d.Name, d.Type, d.AllowsMultiple,
            d.Type == AttributeType.OptionList
                ? d.Options.OrderBy(o => o.Position).Select(o => new Option(o.Id, o.Value)).ToList()
                : null)).ToList());
    }
}
