using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;

namespace Catalog.Api.Features;

/// <summary>
/// Reading, validating and rewriting a Product's Attributes jsonb (ADR 0006): an object keyed by
/// Attribute Definition id. Option lists store option ids (a string, or an array for multi-select).
/// Consistency with the Attribute Definitions is enforced here, not by the database.
/// </summary>
public static class ProductAttributes
{
    public const int TextMaxLength = 500;

    public sealed record AttributeValue(Guid DefinitionId, string Name, AttributeType Type, JsonElement Value);

    /// <summary>Validates raw values against the Tenant's definitions and returns the jsonb document.</summary>
    public static string? Validate(
        IReadOnlyDictionary<string, JsonElement> values,
        IReadOnlyDictionary<Guid, AttributeDefinition> definitions,
        ValidationErrors errors)
    {
        var document = new JsonObject();
        foreach (var (key, value) in values)
        {
            if (!Guid.TryParse(key, out var definitionId) || !definitions.TryGetValue(definitionId, out var definition))
            {
                errors.Add(key, "Unknown Attribute Definition.");
                continue;
            }

            var error = ValidateValue(definition, value);
            if (error is not null)
            {
                errors.Add(key, error);
                continue;
            }

            document[definitionId.ToString()] = JsonNode.Parse(value.GetRawText());
        }

        return errors.HasErrors ? null : document.ToJsonString();
    }

    private static string? ValidateValue(AttributeDefinition definition, JsonElement value)
    {
        switch (definition.Type)
        {
            case AttributeType.Text:
                return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
                    && value.GetString()!.Length <= TextMaxLength
                    ? null
                    : $"A text value must be a non-empty string of at most {TextMaxLength} characters.";
            case AttributeType.Number:
                return value.ValueKind == JsonValueKind.Number ? null : "A number value must be a JSON number.";
            case AttributeType.Boolean:
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "A boolean value must be true or false.";
            case AttributeType.Date:
                return value.ValueKind == JsonValueKind.String
                    && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    ? null
                    : "A date value must be a string formatted as YYYY-MM-DD.";
            case AttributeType.OptionList:
                var optionIds = definition.Options.Select(o => o.Id).ToHashSet();
                if (!definition.AllowsMultiple)
                {
                    return value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id) && optionIds.Contains(id)
                        ? null
                        : "A single-select value must be the id of one of the definition's Options.";
                }

                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                {
                    return "A multi-select value must be a non-empty array of Option ids.";
                }

                var seen = new HashSet<Guid>();
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var itemId)
                        || !optionIds.Contains(itemId) || !seen.Add(itemId))
                    {
                        return "A multi-select value must be an array of distinct ids of the definition's Options.";
                    }
                }

                return null;
            default:
                return "Unsupported attribute type.";
        }
    }

    /// <summary>The Product's Attributes joined with their definitions; values whose definition is gone are skipped.</summary>
    public static List<AttributeValue> Describe(string attributesJson, IReadOnlyDictionary<Guid, AttributeDefinition> definitions)
    {
        using var document = JsonDocument.Parse(attributesJson);
        var result = new List<AttributeValue>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (Guid.TryParse(property.Name, out var id) && definitions.TryGetValue(id, out var definition))
            {
                result.Add(new AttributeValue(id, definition.Name, definition.Type, property.Value.Clone()));
            }
        }

        return result.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>True when the Product's Attributes reference the definition (or, when given, that Option).</summary>
    public static bool Uses(string attributesJson, Guid definitionId, Guid? optionId = null)
    {
        var root = JsonNode.Parse(attributesJson)!.AsObject();
        if (!root.TryGetPropertyValue(definitionId.ToString(), out var value) || value is null)
        {
            return false;
        }

        if (optionId is not { } option)
        {
            return true;
        }

        return value switch
        {
            JsonArray array => array.Any(item => item?.GetValue<string>() == option.ToString()),
            JsonValue single => single.TryGetValue<string>(out var s) && s == option.ToString(),
            _ => false,
        };
    }

    /// <summary>Removes the definition's value, or only that Option (dropping the key when nothing is left).</summary>
    public static string Remove(string attributesJson, Guid definitionId, Guid? optionId = null)
    {
        var root = JsonNode.Parse(attributesJson)!.AsObject();
        var key = definitionId.ToString();
        if (!root.TryGetPropertyValue(key, out var value) || value is null)
        {
            return attributesJson;
        }

        if (optionId is not { } option)
        {
            root.Remove(key);
        }
        else if (value is JsonArray array)
        {
            foreach (var item in array.Where(i => i?.GetValue<string>() == option.ToString()).ToList())
            {
                array.Remove(item);
            }

            if (array.Count == 0)
            {
                root.Remove(key);
            }
        }
        else if (value is JsonValue single && single.TryGetValue<string>(out var s) && s == option.ToString())
        {
            root.Remove(key);
        }

        return root.ToJsonString();
    }
}
