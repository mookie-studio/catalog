using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
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

    private static async Task<Ok<PagedResponse<AttributeDefinitionResponse>>> Handle(
        int? page, int? pageSize, CatalogDbContext db, CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var totalCount = await db.AttributeDefinitions.CountAsync(ct);
        var definitions = await db.AttributeDefinitions
            .Include(d => d.Options)
            .OrderBy(d => d.NormalizedName).ThenBy(d => d.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<AttributeDefinitionResponse>(
            definitions.Select(AttributeDefinitionResponse.From).ToList(), paging.Page, paging.PageSize, totalCount));
    }
}
