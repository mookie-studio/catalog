namespace Catalog.Api.Tests;

public sealed record PagedBody<T>(List<T> Items, int Page, int PageSize, int TotalCount);

public sealed record TenantBody(Guid Id, string Name, string Slug, bool IsActive, DateTimeOffset CreatedAt);

public sealed record AdminBody(Guid Id, Guid UserId, string Email);

public sealed record AdminSession(TenantBody Tenant, AdminBody Admin, HttpClient Client);

public sealed record CurrentTenantBody(Guid Id, string Name, string Slug);
