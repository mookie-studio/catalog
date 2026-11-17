using System.Globalization;
using System.Text.Json.Nodes;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>
/// Product import (see GLOSSARY.md): creates and updates the Tenant's products from a CSV in the export format,
/// matched by SKU. With dryRun (the default) nothing is saved. New products are Drafts with a generated
/// Product slug; status and slug columns are ignored. On updates, a missing column leaves the field untouched
/// and an empty cell clears it (name cannot be cleared). Invalid rows are skipped and reported.
/// </summary>
public static class ImportProducts
{
    public const long MaxBytes = 1024 * 1024;
    public const int MaxRows = 1000;

    public enum Outcome
    {
        Create,
        Update,
        Skip,
    }

    public sealed record RowError(string Column, string Message);

    public sealed record RowReport(int Row, string? Sku, Outcome Outcome, IReadOnlyList<RowError> Errors);

    public sealed record Report(int Created, int Updated, int Skipped, IReadOnlyList<RowReport> Rows, IReadOnlyList<string> Warnings);

    public static RouteGroupBuilder MapImportProducts(this RouteGroupBuilder admin)
    {
        admin.MapPost("/products/import", Handle)
            .WithName("ImportProducts")
            .DisableAntiforgery(); // Bearer-token API: no cookies, so no CSRF exposure.
        return admin;
    }

    /// <summary>A validated row, ready to apply.</summary>
    private sealed class PlannedRow
    {
        public required int Row { get; init; }
        public required string Sku { get; init; }
        public Product? Existing { get; init; }
        public string? Name { get; set; }
        public bool HasDescription { get; set; }
        public string? Description { get; set; }
        public bool? Featured { get; set; }
        public List<Guid>? CategoryIds { get; set; }
        public Dictionary<Guid, JsonNode?> Attributes { get; } = [];
        public List<RowError> Errors { get; } = [];
    }

    private static async Task<Results<Ok<Report>, ValidationProblem>> Handle(
        IFormFile? file, bool? dryRun, CatalogDbContext db, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Invalid("A CSV file is required in the 'file' field.");
        }

        if (file.Length > MaxBytes)
        {
            return Invalid("The file must be at most 1 MB.");
        }

        string[] headers;
        List<string[]> records;
        try
        {
            (headers, records) = await ReadAsync(file, ct);
        }
        catch (CsvHelperException)
        {
            return Invalid("The file is not a valid CSV.");
        }

        if (!headers.Contains(ProductCsv.Sku) || !headers.Contains(ProductCsv.Name))
        {
            return Invalid("The first row must contain the column names, including 'sku' and 'name'.");
        }

        if (records.Count > MaxRows)
        {
            return Invalid($"The file can have at most {MaxRows} products.");
        }

        var definitions = await db.AttributeDefinitions.Include(d => d.Options).AsNoTracking().ToListAsync(ct);
        var definitionsByName = definitions.ToDictionary(d => d.NormalizedName);
        var categoryIdsByPath = ProductCsv.CategoryPaths(await db.Categories.AsNoTracking().ToListAsync(ct))
            .ToDictionary(p => p.Value, p => p.Key);

        var warnings = new List<string>();
        var columns = new Dictionary<string, int>();
        var attributeColumns = new Dictionary<int, AttributeDefinition>();
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            if (ProductCsv.FixedColumns.Contains(header))
            {
                columns.TryAdd(header, i);
            }
            else if (definitionsByName.TryGetValue(AttributeDefinition.Normalize(header), out var definition))
            {
                attributeColumns[i] = definition;
            }
            else
            {
                warnings.Add($"Unknown column '{header}' was ignored.");
            }
        }

        var skus = records.Select(r => Cell(r, columns[ProductCsv.Sku])).Where(s => s.Length > 0).Distinct().ToList();
        var existing = await db.Products.Where(p => skus.Contains(p.Sku)).ToDictionaryAsync(p => p.Sku, ct);

        var planned = new List<PlannedRow>();
        var firstRowBySku = new Dictionary<string, int>();
        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var sku = Cell(record, columns[ProductCsv.Sku]);
            var row = new PlannedRow { Row = index + 1, Sku = sku, Existing = existing.GetValueOrDefault(sku) };
            planned.Add(row);

            var errors = new ValidationErrors();
            ProductRules.ValidateSku(row.Sku, errors);
            if (row.Sku.Length > 0 && !firstRowBySku.TryAdd(row.Sku, row.Row))
            {
                row.Errors.Add(new RowError(ProductCsv.Sku, $"The SKU is repeated; it was first used in row {firstRowBySku[row.Sku]}."));
            }

            var name = Cell(record, columns[ProductCsv.Name]);
            if (name.Length == 0 && row.Existing is not null)
            {
                row.Errors.Add(new RowError(ProductCsv.Name, "The name cannot be cleared."));
            }
            else
            {
                ProductRules.ValidateName(name, errors);
                row.Name = name;
            }

            if (columns.TryGetValue(ProductCsv.Description, out var descriptionColumn))
            {
                var description = Cell(record, descriptionColumn, trim: false);
                ProductRules.ValidateDescription(description, errors);
                row.HasDescription = true;
                row.Description = ProductRules.NormalizeDescription(description);
            }

            if (columns.TryGetValue(ProductCsv.Featured, out var featuredColumn))
            {
                var featured = Cell(record, featuredColumn);
                if (featured.Length == 0)
                {
                    row.Featured = false;
                }
                else if (bool.TryParse(featured, out var flag))
                {
                    row.Featured = flag;
                }
                else
                {
                    row.Errors.Add(new RowError(ProductCsv.Featured, "Featured must be true, false or empty."));
                }
            }

            if (columns.TryGetValue(ProductCsv.Categories, out var categoriesColumn))
            {
                row.CategoryIds = [];
                foreach (var path in ProductCsv.SplitValues(Cell(record, categoriesColumn)))
                {
                    if (categoryIdsByPath.TryGetValue(ProductCsv.NormalizePath(path), out var categoryId))
                    {
                        if (!row.CategoryIds.Contains(categoryId))
                        {
                            row.CategoryIds.Add(categoryId);
                        }
                    }
                    else
                    {
                        row.Errors.Add(new RowError(ProductCsv.Categories, $"Unknown Category '{path}'."));
                    }
                }
            }

            foreach (var (column, definition) in attributeColumns)
            {
                var cell = Cell(record, column);
                if (cell.Length == 0)
                {
                    row.Attributes[definition.Id] = null;
                    continue;
                }

                var (value, error) = ProductCsv.ParseAttribute(definition, cell);
                if (error is not null)
                {
                    row.Errors.Add(new RowError(headers[column], error));
                }
                else
                {
                    row.Attributes[definition.Id] = value;
                }
            }

            foreach (var (field, messages) in errors.ToDictionary())
            {
                row.Errors.AddRange(messages.Select(m => new RowError(field, m)));
            }
        }

        if (dryRun == false)
        {
            await ApplyAsync(planned.Where(r => r.Errors.Count == 0).ToList(), db, ct);
        }

        var rows = planned
            .Select(r => new RowReport(
                r.Row,
                r.Sku.Length == 0 ? null : r.Sku,
                r.Errors.Count > 0 ? Outcome.Skip : r.Existing is null ? Outcome.Create : Outcome.Update,
                r.Errors))
            .ToList();
        return TypedResults.Ok(new Report(
            rows.Count(r => r.Outcome == Outcome.Create),
            rows.Count(r => r.Outcome == Outcome.Update),
            rows.Count(r => r.Outcome == Outcome.Skip),
            rows,
            warnings));
    }

    private static async Task ApplyAsync(List<PlannedRow> rows, CatalogDbContext db, CancellationToken ct)
    {
        var newSlugs = new HashSet<string>(StringComparer.Ordinal);
        var now = Timestamps.UtcNow();
        foreach (var row in rows)
        {
            var product = row.Existing;
            if (product is null)
            {
                var slug = await ProductRules.GenerateUniqueSlugAsync(row.Name!, db, ct, newSlugs);
                newSlugs.Add(slug);
                product = new Product { Name = row.Name!, Sku = row.Sku, Slug = slug };
                db.Products.Add(product);
            }
            else
            {
                product.Name = row.Name!;
                product.UpdatedAt = now;
            }

            if (row.HasDescription)
            {
                product.Description = row.Description;
            }

            if (row.Featured is { } featured)
            {
                product.IsFeatured = featured;
            }

            if (row.Attributes.Count > 0)
            {
                var document = JsonNode.Parse(product.Attributes)!.AsObject();
                foreach (var (definitionId, value) in row.Attributes)
                {
                    document.Remove(definitionId.ToString());
                    if (value is not null)
                    {
                        document[definitionId.ToString()] = value.DeepClone();
                    }
                }

                product.Attributes = document.ToJsonString();
            }

            if (row.CategoryIds is { } categoryIds)
            {
                if (row.Existing is not null)
                {
                    db.ProductCategories.RemoveRange(await db.ProductCategories.Where(l => l.ProductId == product.Id).ToListAsync(ct));
                }

                db.ProductCategories.AddRange(categoryIds.Select(id => new ProductCategory { ProductId = product.Id, CategoryId = id }));
            }
        }

        await db.SaveChangesAsync(ct); // One transaction for the whole import.
    }

    private static async Task<(string[] Headers, List<string[]> Records)> ReadAsync(IFormFile file, CancellationToken ct)
    {
        using var reader = new StreamReader(file.OpenReadStream(), detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = false,
            BadDataFound = null,
            MissingFieldFound = null,
        });
        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            return ([], []);
        }

        var headers = csv.HeaderRecord!.Select(h => h.Trim()).ToArray();
        var records = new List<string[]>();
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            var record = csv.Parser.Record ?? [];
            if (record.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            records.Add(record);
            if (records.Count > MaxRows)
            {
                break;
            }
        }

        return (headers, records);
    }

    private static string Cell(string[] record, int column, bool trim = true)
    {
        var value = column < record.Length ? record[column] : "";
        return trim ? value.Trim() : value;
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new ValidationErrors().Add("file", message).ToDictionary());
}
