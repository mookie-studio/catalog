using System.Text.Json;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>Detail of a Published product by Product slug. Option-list values are returned as their text.</summary>
public static class GetPublicProduct
{
    public sealed record Attribute(string Name, AttributeType Type, JsonElement Value);

    public sealed record Response(string Slug, string Name, string? Description, IReadOnlyList<string> Images, IReadOnlyList<Attribute> Attributes);

    public static RouteGroupBuilder MapGetPublicProduct(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapGet("/products/{productSlug}", Handle).WithName("GetPublicProduct");
        return publicGroup;
    }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        string productSlug, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == productSlug && p.Status == ProductStatus.Published, ct);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        var images = await db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == product.Id)
            .OrderBy(i => i.Position)
            .Select(i => i.StorageKey)
            .ToListAsync(ct);

        var definitions = await db.AttributeDefinitions.Include(d => d.Options).AsNoTracking().ToDictionaryAsync(d => d.Id, ct);
        var attributes = ProductAttributes.Describe(product.Attributes, definitions)
            .Select(a => new Attribute(a.Name, a.Type, ToDisplayValue(definitions[a.DefinitionId], a.Value)))
            .ToList();

        return TypedResults.Ok(new Response(
            product.Slug, product.Name, product.Description, images.Select(storage.GetPublicUrl).ToList(), attributes));
    }

    private static JsonElement ToDisplayValue(AttributeDefinition definition, JsonElement value)
    {
        if (definition.Type != AttributeType.OptionList)
        {
            return value;
        }

        var options = definition.Options.ToDictionary(o => o.Id.ToString());
        if (value.ValueKind == JsonValueKind.Array)
        {
            var texts = value.EnumerateArray()
                .Select(v => options.GetValueOrDefault(v.GetString() ?? ""))
                .Where(o => o is not null)
                .OrderBy(o => o!.Position)
                .Select(o => o!.Value)
                .ToList();
            return JsonSerializer.SerializeToElement(texts);
        }

        return JsonSerializer.SerializeToElement(options.GetValueOrDefault(value.GetString() ?? "")?.Value);
    }
}
