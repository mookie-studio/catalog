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

public sealed record CategoryBody(Guid Id, string Name, List<CategoryBody> Children);

public sealed record ProductCategoriesBody(Guid Id, List<Guid> CategoryIds);

public sealed record ImageBody(Guid Id, string Url, string ThumbnailUrl, int Position);

public sealed record ProductImagesBody(Guid Id, List<ImageBody> Images);

public sealed record PublicProductItem(string Slug, string Name, string? MainImageUrl);

public sealed record PublicAttributeBody(string Name, string Type, System.Text.Json.JsonElement Value);

public sealed record PublicImageBody(string Url, string ThumbnailUrl);

public sealed record PublicProductBody(string Slug, string Name, string? Description, List<PublicImageBody> Images, List<PublicAttributeBody> Attributes);

public sealed record PublicFilterOption(Guid Id, string Value);

public sealed record PublicFilterBody(Guid DefinitionId, string Name, string Type, bool AllowsMultiple, List<PublicFilterOption>? Options);

public sealed record DashboardBody(
    int DraftCount, int PublishedCount, int ArchivedCount, int FeaturedCount, int OpenInquiryCount,
    int PublishedWithoutImagesCount, int PublishedWithoutCategoriesCount, bool HasCategories);

public sealed record BulkFailureBody(Guid ProductId, Dictionary<string, string[]> Errors);

public sealed record BulkResultBody(List<Guid> Succeeded, List<BulkFailureBody> Failed);
