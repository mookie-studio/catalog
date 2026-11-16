namespace Catalog.Api.Tests;

public sealed record PagedBody<T>(List<T> Items, int Page, int PageSize, int TotalCount);

public sealed record TenantBody(Guid Id, string Name, string Slug, bool IsActive, DateTimeOffset CreatedAt);

public sealed record AdminBody(Guid Id, Guid UserId, string Email);

public sealed record AdminSession(TenantBody Tenant, AdminBody Admin, HttpClient Client);

public sealed record CurrentTenantBody(Guid Id, string Name, string Slug);

public sealed record ProductBody(
    Guid Id, string Name, string? Description, string Sku, string Slug, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record ProductListItem(Guid Id, string Name, string Sku, string Slug, string Status, DateTimeOffset UpdatedAt);

public sealed record OptionBody(Guid Id, string Value, int Position);

public sealed record DefinitionBody(
    Guid Id, string Name, string Type, bool IsFilterable, bool AllowsMultiple, DateTimeOffset CreatedAt, List<OptionBody> Options);

public sealed record AttributeValueBody(Guid DefinitionId, string Name, string Type, System.Text.Json.JsonElement Value);

public sealed record ProductDetailBody(Guid Id, string Name, string Slug, string Status, List<AttributeValueBody> Attributes);
