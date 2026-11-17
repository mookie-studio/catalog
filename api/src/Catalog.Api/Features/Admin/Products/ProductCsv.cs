using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Catalog.Api.Data;
using Catalog.Api.Features.Admin.Categories;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>
/// The CSV format shared by Product export and Product import (see GLOSSARY.md): fixed columns, then one
/// column per Attribute Definition named exactly as the definition. Several values are joined by "|";
/// a Category is written as its path joined by " > ".
/// </summary>
public static class ProductCsv
{
    public const string Sku = "sku";
    public const string Name = "name";
    public const string Description = "description";
    public const string Status = "status";
    public const string Slug = "slug";
    public const string Featured = "featured";
    public const string Categories = "categories";

    public static readonly string[] FixedColumns = [Sku, Name, Description, Status, Slug, Featured, Categories];

    public const char Separator = '|';
    public const string PathSeparator = " > ";
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Path of every Category, e.g. "Tools > Power tools", keyed by id.</summary>
    public static Dictionary<Guid, string> CategoryPaths(IReadOnlyCollection<Category> categories)
    {
        var byId = categories.ToDictionary(c => c.Id);
        var paths = new Dictionary<Guid, string>();
        foreach (var category in categories)
        {
            var names = new List<string>();
            for (Category? current = category; current is not null; current = current.ParentId is { } p ? byId.GetValueOrDefault(p) : null)
            {
                names.Add(current.Name);
            }

            names.Reverse();
            paths[category.Id] = string.Join(PathSeparator, names);
        }

        return paths;
    }

    /// <summary>Normalizes a path written by a person: trims every segment.</summary>
    public static string NormalizePath(string path) =>
        string.Join(PathSeparator, path.Split('>').Select(s => s.Trim()));

    public static string FormatBoolean(bool value) => value ? "true" : "false";

    /// <summary>The cell text of an Attribute value (Option values instead of ids).</summary>
    public static string FormatAttribute(AttributeDefinition definition, JsonElement value)
    {
        switch (definition.Type)
        {
            case AttributeType.Number:
                return value.GetRawText();
            case AttributeType.Boolean:
                return FormatBoolean(value.GetBoolean());
            case AttributeType.OptionList:
                var options = definition.Options.ToDictionary(o => o.Id.ToString());
                var ids = value.ValueKind == JsonValueKind.Array
                    ? value.EnumerateArray().Select(v => v.GetString() ?? "")
                    : [value.GetString() ?? ""];
                return string.Join(Separator, ids
                    .Select(id => options.GetValueOrDefault(id))
                    .Where(o => o is not null)
                    .OrderBy(o => o!.Position)
                    .Select(o => o!.Value));
            default:
                return value.GetString() ?? "";
        }
    }

    /// <summary>Splits a multi-value cell on "|", trimming and dropping empty parts.</summary>
    public static List<string> SplitValues(string cell) =>
        cell.Split(Separator).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();

    /// <summary>Parses a non-empty cell into the jsonb value of an Attribute, or returns an error message.</summary>
    public static (JsonNode? Value, string? Error) ParseAttribute(AttributeDefinition definition, string cell)
    {
        switch (definition.Type)
        {
            case AttributeType.Text:
                return cell.Length <= ProductAttributes.TextMaxLength
                    ? (JsonValue.Create(cell), null)
                    : (null, $"A text value must be at most {ProductAttributes.TextMaxLength} characters.");
            case AttributeType.Number:
                return decimal.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    ? (JsonNode.Parse(number.ToString(CultureInfo.InvariantCulture)), null)
                    : (null, "A number must use '.' as decimal separator, for example 12.5.");
            case AttributeType.Boolean:
                return bool.TryParse(cell, out var flag)
                    ? (JsonValue.Create(flag), null)
                    : (null, "A boolean must be true or false.");
            case AttributeType.Date:
                return DateOnly.TryParseExact(cell, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    ? (JsonValue.Create(cell), null)
                    : (null, "A date must be formatted as YYYY-MM-DD.");
            case AttributeType.OptionList:
                var values = definition.AllowsMultiple ? SplitValues(cell) : [cell];
                var ids = new List<string>();
                foreach (var text in values)
                {
                    var option = definition.Options.FirstOrDefault(o => string.Equals(o.Value, text, StringComparison.OrdinalIgnoreCase));
                    if (option is null)
                    {
                        return (null, $"'{text}' is not an Option of {definition.Name}.");
                    }

                    if (!ids.Contains(option.Id.ToString()))
                    {
                        ids.Add(option.Id.ToString());
                    }
                }

                return definition.AllowsMultiple
                    ? (new JsonArray(ids.Select(id => (JsonNode?)JsonValue.Create(id)).ToArray()), null)
                    : (JsonValue.Create(ids[0]), null);
            default:
                return (null, "Unsupported attribute type.");
        }
    }
}
