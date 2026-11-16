using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Catalog.Api.Data;
using Catalog.Api.Features;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Rules and response shape shared by the admin Product slices.</summary>
public static partial class ProductRules
{
    public const int GeneratedSlugMaxLength = 80;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    /// <summary>
    /// Lowercase, remove diacritics, replace runs of non [a-z0-9] with "-", trim "-", cut to 80 chars.
    /// </summary>
    public static string Slugify(string name)
    {
        var decomposed = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutDiacritics = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                withoutDiacritics.Append(c);
            }
        }

        var slug = NonSlugChars().Replace(withoutDiacritics.ToString().Normalize(NormalizationForm.FormC), "-").Trim('-');
        if (slug.Length > GeneratedSlugMaxLength)
        {
            slug = slug[..GeneratedSlugMaxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? "product" : slug;
    }

    /// <summary>A Product slug unique in the current Tenant: the base slug, or base-2, base-3, ...</summary>
    public static async Task<string> GenerateUniqueSlugAsync(string name, CatalogDbContext db, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        var taken = await db.Products
            .Where(p => p.Slug == baseSlug || p.Slug.StartsWith(baseSlug + "-"))
            .Select(p => p.Slug)
            .ToListAsync(ct);
        var takenSet = taken.ToHashSet(StringComparer.Ordinal);

        if (!takenSet.Contains(baseSlug))
        {
            return baseSlug;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!takenSet.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    public static void ValidateName(string? name, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "The name is required.");
        }
        else if (name.Trim().Length > Product.NameMaxLength)
        {
            errors.Add("name", $"The name must be at most {Product.NameMaxLength} characters.");
        }
    }

    public static void ValidateSku(string? sku, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add("sku", "The SKU is required.");
        }
        else if (sku.Trim().Length > Product.SkuMaxLength)
        {
            errors.Add("sku", $"The SKU must be at most {Product.SkuMaxLength} characters.");
        }
    }

    public static void ValidateDescription(string? description, ValidationErrors errors)
    {
        if (description is not null && description.Length > Product.DescriptionMaxLength)
        {
            errors.Add("description", $"The description must be at most {Product.DescriptionMaxLength} characters.");
        }
    }

    public static void ValidateSlug(string? slug, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            errors.Add("slug", "The Product slug is required.");
        }
        else if (slug.Length > Product.SlugMaxLength)
        {
            errors.Add("slug", $"The Product slug must be at most {Product.SlugMaxLength} characters.");
        }
        else if (!SlugPattern().IsMatch(slug))
        {
            errors.Add("slug", "The Product slug may only contain lowercase letters, digits and single hyphens between them.");
        }
    }

    /// <summary>Publishing requires name, SKU and a non-empty description.</summary>
    public static void ValidatePublishable(string? name, string? sku, string? description, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("description", "A Published product requires a description.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "A Published product requires a name.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add("sku", "A Published product requires a SKU.");
        }
    }

    public static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}

public sealed record ProductResponse(
    Guid Id, string Name, string? Description, string Sku, string Slug, ProductStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<ProductAttributes.AttributeValue> Attributes,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<ProductImageResponse> Images)
{
    public static ProductResponse From(
        Product p, IReadOnlyList<ProductAttributes.AttributeValue> attributes, IReadOnlyList<Guid> categoryIds,
        IReadOnlyList<ProductImageResponse> images) =>
        new(p.Id, p.Name, p.Description, p.Sku, p.Slug, p.Status, p.CreatedAt, p.UpdatedAt, attributes, categoryIds, images);

    /// <summary>Builds the full response, joining the Product's Attributes with the Tenant's definitions.</summary>
    public static async Task<ProductResponse> LoadAsync(Product p, CatalogDbContext db, IImageStorage storage, CancellationToken ct)
    {
        var definitions = await db.AttributeDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Id, ct);
        var categoryIds = await db.ProductCategories.Where(l => l.ProductId == p.Id)
            .OrderBy(l => l.CategoryId).Select(l => l.CategoryId).ToListAsync(ct);
        var images = await ProductImages.ListAsync(p.Id, db, storage, ct);
        return From(p, ProductAttributes.Describe(p.Attributes, definitions), categoryIds, images);
    }
}
