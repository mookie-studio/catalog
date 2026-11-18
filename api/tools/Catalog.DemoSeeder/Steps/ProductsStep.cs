using System.Text.Json;
using System.Text.Json.Nodes;
using Catalog.DemoSeeder.Dataset;

namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Brings a Demo tenant's products to the dataset, matched by SKU: creates the missing ones, updates what
/// differs, and moves products that are not in the dataset to Archived (products are never deleted).
/// </summary>
public static class ProductsStep
{
    public static async Task RunAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        var listed = await state.Admin.GetAllPagesAsync<ProductListItem>("/api/admin/products", ct);
        var bySku = listed.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);

        foreach (var wanted in state.Demo.Products)
        {
            ProductResponse product;
            if (bySku.TryGetValue(wanted.Sku, out var item))
            {
                product = await state.Admin.GetAsync<ProductResponse>($"/api/admin/products/{item.Id}", ct);
            }
            else
            {
                product = await state.Admin.PostAsync<ProductResponse>(
                    "/api/admin/products", new { name = wanted.Name, sku = wanted.Sku, description = wanted.Description }, ct);
                context.Log.Change(state.Slug, "product created", wanted.Sku);
            }

            state.Products[wanted.Sku] = await ConvergeAsync(context, state, product, wanted, ct);
        }

        var extra = listed
            .Where(p => p.Status != "Archived" && !state.Products.ContainsKey(p.Sku))
            .Select(p => p.Id)
            .ToList();
        foreach (var batch in extra.Chunk(100))
        {
            await state.Admin.PostAsync<JsonElement>("/api/admin/products/bulk", new { productIds = batch, action = "Archive" }, ct);
            context.Log.Change(state.Slug, "products archived", $"{batch.Length} not in the dataset");
        }
    }

    private static async Task<ProductResponse> ConvergeAsync(
        SeedContext context, TenantState state, ProductResponse product, ProductEntry wanted, CancellationToken ct)
    {
        var path = $"/api/admin/products/{product.Id}";

        // A Published product must keep a description: leave Published before the text changes, publish after.
        if (product.Status != wanted.Status && product.Status == "Published")
        {
            await ChangeStatusAsync();
        }

        if (product.Name != wanted.Name || product.Description != wanted.Description || product.Sku != wanted.Sku)
        {
            product = await state.Admin.PutAsync<ProductResponse>(
                path, new { name = wanted.Name, description = wanted.Description, sku = wanted.Sku, slug = product.Slug }, ct);
            context.Log.Change(state.Slug, "product updated", $"{wanted.Sku} text");
        }

        var values = AttributeValues(state, wanted);
        if (!SameAttributes(product.Attributes, values))
        {
            product = await state.Admin.PutAsync<ProductResponse>($"{path}/attributes", new { values }, ct);
            context.Log.Change(state.Slug, "product updated", $"{wanted.Sku} attributes");
        }

        var categoryIds = wanted.CategoryList.Select(c => state.Categories[c]).ToList();
        if (!categoryIds.ToHashSet().SetEquals(product.CategoryIds))
        {
            product = await state.Admin.PutAsync<ProductResponse>($"{path}/categories", new { categoryIds }, ct);
            context.Log.Change(state.Slug, "product updated", $"{wanted.Sku} categories");
        }

        if (product.IsFeatured != wanted.Featured)
        {
            product = await state.Admin.PutAsync<ProductResponse>($"{path}/featured", new { featured = wanted.Featured }, ct);
            context.Log.Change(state.Slug, "product updated", $"{wanted.Sku} featured");
        }

        if (product.Status != wanted.Status)
        {
            await ChangeStatusAsync();
        }

        return await state.Admin.GetAsync<ProductResponse>(path, ct);

        async Task ChangeStatusAsync()
        {
            await state.Admin.PostAsync($"{path}/status", new { status = wanted.Status }, ct);
            product = product with { Status = wanted.Status };
            context.Log.Change(state.Slug, "product updated", $"{wanted.Sku} status {wanted.Status}");
        }
    }

    /// <summary>The attribute map the API expects: keyed by definition id, Options given by id.</summary>
    private static Dictionary<string, JsonNode?> AttributeValues(TenantState state, ProductEntry wanted)
    {
        var values = new Dictionary<string, JsonNode?>();
        foreach (var (name, value) in wanted.AttributeMap)
        {
            var definition = state.Definitions[name];
            values[definition.Id.ToString()] = definition.Type != "OptionList"
                ? JsonNode.Parse(value.GetRawText())
                : value.ValueKind == JsonValueKind.Array
                    ? new JsonArray(value.EnumerateArray().Select(v => (JsonNode?)JsonValue.Create(OptionId(definition, v.GetString()!))).ToArray())
                    : JsonValue.Create(OptionId(definition, value.GetString()!));
        }

        return values;
    }

    private static string OptionId(DefinitionResponse definition, string text) =>
        definition.Options.First(o => string.Equals(o.Value, text, StringComparison.OrdinalIgnoreCase)).Id.ToString();

    private static bool SameAttributes(List<ProductAttributeValue> current, Dictionary<string, JsonNode?> wanted)
    {
        if (current.Count != wanted.Count)
        {
            return false;
        }

        foreach (var value in current)
        {
            if (!wanted.TryGetValue(value.DefinitionId.ToString(), out var node) || Normalize(JsonNode.Parse(value.Value.GetRawText())) != Normalize(node))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A comparable text form: numbers as decimals, multi-select arrays in a stable order.</summary>
    private static string Normalize(JsonNode? node) => node switch
    {
        JsonArray array => "[" + string.Join(",", array.Select(Normalize).Order(StringComparer.Ordinal)) + "]",
        JsonValue value when value.GetValueKind() == JsonValueKind.Number => value.GetValue<decimal>().ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => node?.ToJsonString() ?? "null",
    };
}
