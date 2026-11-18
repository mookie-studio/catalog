using System.Globalization;
using System.Text.Json;

namespace Catalog.DemoSeeder.Dataset;

/// <summary>The Demo dataset is not valid; every problem is listed with its file and JSON path.</summary>
public sealed class DatasetException(IReadOnlyList<string> errors)
    : Exception("The Demo dataset is not valid:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Reads the Demo dataset folder and validates all of it before the seeder changes anything.</summary>
public static class DatasetLoader
{
    public const string FolderName = "demo";
    public const int MaxImagesPerProduct = 5;
    public const long MaxImageBytes = 2 * 1024 * 1024;
    public static readonly string[] Statuses = ["Draft", "Published", "Archived"];
    public static readonly string[] Types = ["Text", "Number", "Boolean", "Date", "OptionList"];
    public static readonly string[] InquiryStatuses = ["Open", "Handled"];
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>The first <c>demo</c> folder found walking up from <paramref name="start"/>.</summary>
    public static string? FindDefault(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, FolderName);
            if (File.Exists(Path.Combine(candidate, "tenants.json")))
            {
                return candidate;
            }
        }

        return null;
    }

    public static DemoDataset Load(string root)
    {
        var errors = new List<string>();
        var tenantsFile = Path.Combine(root, "tenants.json");
        var entries = Read<List<TenantEntry>>(tenantsFile, errors) ?? [];
        var tenants = new List<DemoTenant>();

        var slugs = new HashSet<string>(StringComparer.Ordinal);
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var at = $"tenants.json $[{i}]";
            if (string.IsNullOrWhiteSpace(entry.Slug) || string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.AdminEmail))
            {
                errors.Add($"{at}: slug, name and adminEmail are required.");
                continue;
            }

            if (!slugs.Add(entry.Slug))
            {
                errors.Add($"{at}.slug: duplicate Tenant slug '{entry.Slug}'.");
            }

            if (!emails.Add(entry.AdminEmail))
            {
                errors.Add($"{at}.adminEmail: '{entry.AdminEmail}' is the Admin of another Demo tenant.");
            }

            tenants.Add(LoadTenant(root, entry, errors));
        }

        if (errors.Count > 0)
        {
            throw new DatasetException(errors);
        }

        return new DemoDataset(root, tenants);
    }

    private static DemoTenant LoadTenant(string root, TenantEntry entry, List<string> errors)
    {
        var folder = Path.Combine(root, entry.Slug);
        var images = Path.Combine(root, "images", entry.Slug);
        var slug = entry.Slug;

        var profile = Read<ProfileFile>(Path.Combine(folder, "profile.json"), errors, optional: true);
        var catalog = Read<CatalogFile>(Path.Combine(folder, "catalog.json"), errors, optional: true) ?? new CatalogFile(null, null);
        var products = Read<List<ProductEntry>>(Path.Combine(folder, "products.json"), errors, optional: true) ?? [];
        var activity = Read<ActivityFile>(Path.Combine(folder, "activity.json"), errors, optional: true) ?? ActivityFile.Empty;

        if (profile?.Logo is { } logo)
        {
            CheckImage(images, logo, $"{slug}/profile.json $.logo", errors);
        }

        var definitions = ValidateCatalog(slug, catalog, errors);
        var paths = catalog.CategoryPaths().ToHashSet(StringComparer.Ordinal);
        ValidateProducts(slug, products, paths, definitions, images, errors);
        ValidateActivity(slug, activity, products, errors);

        return new DemoTenant(entry, profile, catalog, products, activity, images);
    }

    private static Dictionary<string, AttributeEntry> ValidateCatalog(string slug, CatalogFile catalog, List<string> errors)
    {
        var file = $"{slug}/catalog.json";
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in catalog.CategoryPaths())
        {
            if (path.Split('/').Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"{file} $.categories: a Category name is empty.");
            }
            else if (!seenPaths.Add(path))
            {
                errors.Add($"{file} $.categories: duplicate Category '{path}'.");
            }
        }

        var definitions = new Dictionary<string, AttributeEntry>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < catalog.AttributeList.Count; i++)
        {
            var attribute = catalog.AttributeList[i];
            var at = $"{file} $.attributes[{i}]";
            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                errors.Add($"{at}.name: required.");
                continue;
            }

            if (!definitions.TryAdd(attribute.Name, attribute))
            {
                errors.Add($"{at}.name: duplicate Attribute Definition '{attribute.Name}'.");
            }

            if (!Types.Contains(attribute.Type))
            {
                errors.Add($"{at}.type: must be one of {string.Join(", ", Types)}.");
            }
            else if (attribute.Type == "OptionList")
            {
                if (attribute.OptionList.Count == 0)
                {
                    errors.Add($"{at}.options: an option list needs at least one Option.");
                }

                if (attribute.OptionList.Distinct(StringComparer.OrdinalIgnoreCase).Count() != attribute.OptionList.Count)
                {
                    errors.Add($"{at}.options: duplicate Option.");
                }
            }
            else if (attribute.OptionList.Count > 0 || attribute.AllowsMultiple)
            {
                errors.Add($"{at}: only option lists have options or allowsMultiple.");
            }
        }

        return definitions;
    }

    private static void ValidateProducts(
        string slug, List<ProductEntry> products, HashSet<string> paths, Dictionary<string, AttributeEntry> definitions,
        string images, List<string> errors)
    {
        var file = $"{slug}/products.json";
        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var at = $"{file} $[{i}]";
            if (string.IsNullOrWhiteSpace(product.Sku) || string.IsNullOrWhiteSpace(product.Name))
            {
                errors.Add($"{at}: sku and name are required.");
                continue;
            }

            if (!skus.Add(product.Sku))
            {
                errors.Add($"{at}.sku: duplicate SKU '{product.Sku}'.");
            }

            if (!Statuses.Contains(product.Status))
            {
                errors.Add($"{at}.status: must be Draft, Published or Archived.");
            }
            else if (product.Status == "Published" && string.IsNullOrWhiteSpace(product.Description))
            {
                errors.Add($"{at}.description: a Published product needs a description.");
            }

            foreach (var category in product.CategoryList)
            {
                if (!paths.Contains(category))
                {
                    errors.Add($"{at}.categories: '{category}' is not a Category of catalog.json.");
                }
            }

            foreach (var (name, value) in product.AttributeMap)
            {
                if (!definitions.TryGetValue(name, out var definition))
                {
                    errors.Add($"{at}.attributes.{name}: not an Attribute Definition of catalog.json.");
                }
                else if (ValueError(definition, value) is { } error)
                {
                    errors.Add($"{at}.attributes.{name}: {error}");
                }
            }

            if (product.ImageList.Count > MaxImagesPerProduct)
            {
                errors.Add($"{at}.images: at most {MaxImagesPerProduct} Images per product.");
            }

            for (var j = 0; j < product.ImageList.Count; j++)
            {
                CheckImage(images, product.ImageList[j], $"{at}.images[{j}]", errors);
            }
        }
    }

    private static string? ValueError(AttributeEntry definition, JsonElement value) => definition.Type switch
    {
        "Text" => value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? null : "expected a non-empty string.",
        "Number" => value.ValueKind == JsonValueKind.Number ? null : "expected a number.",
        "Boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "expected true or false.",
        "Date" => value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                ? null
                : "expected a date formatted as yyyy-MM-dd.",
        "OptionList" when definition.AllowsMultiple => value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0
            && value.EnumerateArray().All(v => IsOption(definition, v))
            && value.EnumerateArray().Select(v => v.GetString()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.GetArrayLength()
                ? null
                : $"expected a non-empty array of distinct Options ({string.Join(", ", definition.OptionList)}).",
        "OptionList" => IsOption(definition, value) ? null : $"expected one Option ({string.Join(", ", definition.OptionList)}).",
        _ => null,
    };

    private static bool IsOption(AttributeEntry definition, JsonElement value) =>
        value.ValueKind == JsonValueKind.String && definition.OptionList.Contains(value.GetString()!, StringComparer.OrdinalIgnoreCase);

    private static void ValidateActivity(string slug, ActivityFile activity, List<ProductEntry> products, List<string> errors)
    {
        var file = $"{slug}/activity.json";
        var published = products.Where(p => p.Status == "Published").Select(p => p.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < activity.InquiryList.Count; i++)
        {
            var inquiry = activity.InquiryList[i];
            var at = $"{file} $.inquiries[{i}]";
            if (!published.Contains(inquiry.Sku ?? ""))
            {
                errors.Add($"{at}.sku: '{inquiry.Sku}' is not a Published product of products.json.");
            }

            if (!InquiryStatuses.Contains(inquiry.Status))
            {
                errors.Add($"{at}.status: must be Open or Handled.");
            }

            if (string.IsNullOrWhiteSpace(inquiry.Name) || string.IsNullOrWhiteSpace(inquiry.Email) || string.IsNullOrWhiteSpace(inquiry.Message))
            {
                errors.Add($"{at}: name, email and message are required.");
            }

            if (inquiry.DaysAgo < 0)
            {
                errors.Add($"{at}.daysAgo: must be 0 or more.");
            }
        }

        if (activity.Views is { } views && (views.Days is < 1 or > 366 || views.MinPerDay < 0 || views.MaxPerDay < views.MinPerDay))
        {
            errors.Add($"{file} $.views: days must be 1 to 366 and 0 <= minPerDay <= maxPerDay.");
        }
    }

    private static void CheckImage(string directory, string fileName, string at, List<string> errors)
    {
        var path = Path.Combine(directory, fileName);
        if (!ImageExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
        {
            errors.Add($"{at}: '{fileName}' must be a .jpg, .png or .webp file.");
        }
        else if (!File.Exists(path))
        {
            errors.Add($"{at}: '{fileName}' was not found in {directory}.");
        }
        else if (new FileInfo(path).Length > MaxImageBytes)
        {
            errors.Add($"{at}: '{fileName}' is larger than 2 MB.");
        }
    }

    private static T? Read<T>(string path, List<string> errors, bool optional = false) where T : class
    {
        if (!File.Exists(path))
        {
            if (!optional)
            {
                errors.Add($"{path}: file not found.");
            }

            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json);
            if (value is null)
            {
                errors.Add($"{path}: the file is empty.");
            }

            return value;
        }
        catch (JsonException e)
        {
            errors.Add($"{path} {e.Path}: {e.Message}");
            return null;
        }
    }
}
