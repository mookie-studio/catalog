namespace Catalog.Api.Tests;

public sealed record PagedBody<T>(List<T> Items, int Page, int PageSize, int TotalCount);

public sealed record TenantBody(Guid Id, string Name, string Slug, bool IsActive, DateTimeOffset CreatedAt);
