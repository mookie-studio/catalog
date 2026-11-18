using System.Text.Json;

namespace Catalog.DemoSeeder.Dataset;

/// <summary>The whole Demo dataset (GLOSSARY.md): one entry per Demo tenant.</summary>
public sealed record DemoDataset(string Root, IReadOnlyList<DemoTenant> Tenants);

/// <summary>Everything the Demo dataset says about one Demo tenant.</summary>
public sealed record DemoTenant(
    TenantEntry Tenant,
    ProfileFile? Profile,
    CatalogFile Catalog,
    IReadOnlyList<ProductEntry> Products,
    ActivityFile Activity,
    string ImagesDirectory)
{
    public string Slug => Tenant.Slug;

    public string ImagePath(string fileName) => Path.Combine(ImagesDirectory, fileName);
}

/// <summary>One row of <c>demo/tenants.json</c>.</summary>
public sealed record TenantEntry(string Slug, string Name, bool IsActive, string AdminEmail);

/// <summary><c>demo/&lt;slug&gt;/profile.json</c>: the Tenant profile, plus whether Inquiries are enabled and the Logo file.</summary>
public sealed record ProfileFile(
    string? Description,
    string? ContactEmail,
    string? Phone,
    string? Address,
    string? WebsiteUrl,
    string? BrandColor,
    bool InquiriesEnabled,
    string? Logo);

/// <summary><c>demo/&lt;slug&gt;/catalog.json</c>: the Category tree and the Attribute Definitions.</summary>
public sealed record CatalogFile(IReadOnlyList<CategoryEntry>? Categories, IReadOnlyList<AttributeEntry>? Attributes)
{
    public IReadOnlyList<CategoryEntry> CategoryList => Categories ?? [];
    public IReadOnlyList<AttributeEntry> AttributeList => Attributes ?? [];

    /// <summary>Every Category path ("Root/Child") of the tree.</summary>
    public IEnumerable<string> CategoryPaths() => Paths(CategoryList, null);

    private static IEnumerable<string> Paths(IReadOnlyList<CategoryEntry> entries, string? parent)
    {
        foreach (var entry in entries)
        {
            var path = parent is null ? entry.Name : $"{parent}/{entry.Name}";
            yield return path;
            foreach (var child in Paths(entry.ChildList, path))
            {
                yield return child;
            }
        }
    }
}

public sealed record CategoryEntry(string Name, IReadOnlyList<CategoryEntry>? Children)
{
    public IReadOnlyList<CategoryEntry> ChildList => Children ?? [];
}

/// <summary>An Attribute Definition; <c>type</c> is Text, Number, Boolean, Date or OptionList.</summary>
public sealed record AttributeEntry(
    string Name, string Type, bool IsFilterable, bool AllowsMultiple, IReadOnlyList<string>? Options)
{
    public IReadOnlyList<string> OptionList => Options ?? [];
}

/// <summary>One product of <c>demo/&lt;slug&gt;/products.json</c>. Option values are given by Option text.</summary>
public sealed record ProductEntry(
    string Sku,
    string Name,
    string? Description,
    string Status,
    bool Featured,
    IReadOnlyList<string>? Categories,
    IReadOnlyDictionary<string, JsonElement>? Attributes,
    IReadOnlyList<string>? Images)
{
    public IReadOnlyList<string> CategoryList => Categories ?? [];
    public IReadOnlyDictionary<string, JsonElement> AttributeMap => Attributes ?? new Dictionary<string, JsonElement>();
    public IReadOnlyList<string> ImageList => Images ?? [];
}

/// <summary><c>demo/&lt;slug&gt;/activity.json</c>: the Demo activity.</summary>
public sealed record ActivityFile(IReadOnlyList<InquiryEntry>? Inquiries, ViewsEntry? Views)
{
    public static readonly ActivityFile Empty = new(null, null);

    public IReadOnlyList<InquiryEntry> InquiryList => Inquiries ?? [];
}

public sealed record InquiryEntry(string Sku, string Name, string Email, string Message, string Status, int DaysAgo);

/// <summary>Product views of every Published product for the last <c>Days</c> days.</summary>
public sealed record ViewsEntry(int Days, int MinPerDay, int MaxPerDay);
