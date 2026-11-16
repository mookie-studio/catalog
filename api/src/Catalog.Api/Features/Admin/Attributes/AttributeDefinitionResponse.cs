using Catalog.Api.Data;

namespace Catalog.Api.Features.Admin.Attributes;

public sealed record AttributeOptionResponse(Guid Id, string Value, int Position);

public sealed record AttributeDefinitionResponse(
    Guid Id, string Name, AttributeType Type, bool IsFilterable, bool AllowsMultiple,
    DateTimeOffset CreatedAt, IReadOnlyList<AttributeOptionResponse> Options)
{
    public static AttributeDefinitionResponse From(AttributeDefinition d) => new(
        d.Id, d.Name, d.Type, d.IsFilterable, d.AllowsMultiple, d.CreatedAt,
        d.Options.OrderBy(o => o.Position).Select(o => new AttributeOptionResponse(o.Id, o.Value, o.Position)).ToList());
}
