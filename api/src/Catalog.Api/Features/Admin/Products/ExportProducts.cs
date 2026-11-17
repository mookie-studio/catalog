using System.Globalization;
using System.Text;
using System.Text.Json;
using Catalog.Api.Data;
using CsvHelper;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Product export: every product of the Tenant, in any status, as CSV (format in ProductCsv).</summary>
public static class ExportProducts
{
    public static RouteGroupBuilder MapExportProducts(this RouteGroupBuilder admin)
    {
        admin.MapGet("/products/export", Handle)
            .WithName("ExportProducts")
            .Produces<string>(StatusCodes.Status200OK, "text/csv");
        return admin;
    }

    private static async Task<IResult> Handle(CatalogDbContext db, CancellationToken ct)
    {
        var definitions = await db.AttributeDefinitions.Include(d => d.Options).AsNoTracking()
            .OrderBy(d => d.NormalizedName).ThenBy(d => d.Id).ToListAsync(ct);
        var paths = ProductCsv.CategoryPaths(await db.Categories.AsNoTracking().ToListAsync(ct));
        var categoriesByProduct = (await db.ProductCategories.AsNoTracking().ToListAsync(ct))
            .ToLookup(l => l.ProductId, l => l.CategoryId);
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Sku).ToListAsync(ct);

        using var buffer = new MemoryStream();
        await using (var writer = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var column in ProductCsv.FixedColumns)
            {
                csv.WriteField(column);
            }

            foreach (var definition in definitions)
            {
                csv.WriteField(definition.Name);
            }

            await csv.NextRecordAsync();

            foreach (var product in products)
            {
                csv.WriteField(product.Sku);
                csv.WriteField(product.Name);
                csv.WriteField(product.Description ?? "");
                csv.WriteField(product.Status.ToString());
                csv.WriteField(product.Slug);
                csv.WriteField(ProductCsv.FormatBoolean(product.IsFeatured));
                csv.WriteField(string.Join(ProductCsv.Separator, categoriesByProduct[product.Id]
                    .Select(id => paths.GetValueOrDefault(id))
                    .Where(p => p is not null)
                    .Order(StringComparer.OrdinalIgnoreCase)));

                using var attributes = JsonDocument.Parse(product.Attributes);
                foreach (var definition in definitions)
                {
                    csv.WriteField(attributes.RootElement.TryGetProperty(definition.Id.ToString(), out var value)
                        ? ProductCsv.FormatAttribute(definition, value)
                        : "");
                }

                await csv.NextRecordAsync();
            }
        }

        return Results.File(buffer.ToArray(), "text/csv; charset=utf-8", "products.csv");
    }
}
