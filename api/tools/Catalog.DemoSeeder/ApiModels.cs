using System.Text.Json;

namespace Catalog.DemoSeeder;

// The Catalog API's responses, as far as the Demo seeder reads them. The contract is api/openapi/catalog.json.

public sealed record TenantResponse(Guid Id, string Name, string Slug, bool IsActive);

public sealed record AdminResponse(Guid Id, Guid UserId, string Email);

public sealed record CurrentTenantResponse(Guid Id, string Name, string Slug);

public sealed record TenantProfileResponse(
    string Name,
    string Slug,
    string? Description,
    string? ContactEmail,
    string? Phone,
    string? Address,
    string? WebsiteUrl,
    string? BrandColor,
    string? LogoUrl,
    bool InquiriesEnabled);

public sealed record CategoryNode(Guid Id, string Name, List<CategoryNode> Children);

public sealed record OptionResponse(Guid Id, string Value, int Position);

public sealed record DefinitionResponse(Guid Id, string Name, string Type, bool IsFilterable, bool AllowsMultiple, List<OptionResponse> Options);

public sealed record ProductListItem(Guid Id, string Name, string Sku, string Slug, string Status, bool IsFeatured);

public sealed record ProductAttributeValue(Guid DefinitionId, string Name, string Type, JsonElement Value);

public sealed record ProductImageResponse(Guid Id, string Url, string ThumbnailUrl, int Position);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    string Slug,
    string Status,
    List<ProductAttributeValue> Attributes,
    List<Guid> CategoryIds,
    List<ProductImageResponse> Images,
    bool IsFeatured);
