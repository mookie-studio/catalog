using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class ProductCsvTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static async Task<List<string[]>> Export(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/products/export");
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("products.csv", response.Content.Headers.ContentDisposition!.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        using var reader = new StringReader(Encoding.UTF8.GetString(bytes[3..]));
        using var csv = new CsvParser(reader, CultureInfo.InvariantCulture);
        var rows = new List<string[]>();
        while (await csv.ReadAsync())
        {
            rows.Add(csv.Record!);
        }

        return rows;
    }

    private static MultipartFormDataContent CsvForm(string content) =>
        TestImages.Form(Encoding.UTF8.GetBytes(content), "products.csv", "text/csv");

    private static async Task<ImportReport> Import(HttpClient client, string content, bool dryRun)
    {
        var response = await client.PostAsync($"/api/admin/products/import?dryRun={dryRun.ToString().ToLowerInvariant()}", CsvForm(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ImportReport>())!;
    }

    private async Task<AdminSession> TenantWithCatalog()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var brand = await CreateDefinition(a.Client, "Brand", "Text");
        var power = await CreateDefinition(a.Client, "Power (W)", "Number");
        var cordless = await CreateDefinition(a.Client, "Cordless", "Boolean");
        var released = await CreateDefinition(a.Client, "Released", "Date");
        var color = await CreateDefinition(a.Client, "Color", "OptionList", options: ["Red", "Blue"]);
        var sizes = await CreateDefinition(a.Client, "Sizes", "OptionList", allowsMultiple: true, options: ["S", "M", "L"]);
        var tools = await CreateCategory(a.Client, "Tools");
        var power_tools = await CreateCategory(a.Client, "Power tools", tools.Id);
        var garden = await CreateCategory(a.Client, "Garden");

        var drill = await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1", "Drills, \"hard\" walls\nand more");
        await a.Client.PutAsJsonAsync($"/api/admin/products/{drill.Id}/attributes", new
        {
            values = new Dictionary<string, object>
            {
                [brand.Id.ToString()] = "Acme",
                [power.Id.ToString()] = 500.5,
                [cordless.Id.ToString()] = true,
                [released.Id.ToString()] = "2026-01-31",
                [color.Id.ToString()] = color.Options[0].Id,
                [sizes.Id.ToString()] = new[] { sizes.Options[2].Id, sizes.Options[0].Id },
            },
        });
        await a.Client.PutAsJsonAsync($"/api/admin/products/{drill.Id}/categories", new { categoryIds = new[] { power_tools.Id, garden.Id } });
        await a.Client.PutAsJsonAsync($"/api/admin/products/{drill.Id}/featured", new { featured = true });
        await CreateProduct(a.Client, "Circular saw", "CS-1");
        return a;
    }

    [Fact]
    public async Task Export_writes_every_product_with_attributes_and_category_paths()
    {
        var a = await TenantWithCatalog();

        var rows = await Export(a.Client);

        Assert.Equal(
            ["sku", "name", "description", "status", "slug", "featured", "categories", "Brand", "Color", "Cordless", "Power (W)", "Released", "Sizes"],
            rows[0]);
        Assert.Equal(["CS-1", "Circular saw", "", "Draft", "circular-saw", "false", "", "", "", "", "", "", ""], rows[1]);
        Assert.Equal(
            ["HD-1", "Hammer drill", "Drills, \"hard\" walls\nand more", "Published", "hammer-drill", "true", "Garden|Tools > Power tools",
             "Acme", "Red", "true", "500.5", "2026-01-31", "S|L"],
            rows[2]);
    }

    [Fact]
    public async Task Isolation_tenant_Bs_products_and_definitions_never_appear_in_tenant_As_export()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateDefinition(b.Client, "Brand", "Text");
        await CreateProduct(b.Client, "Hammer drill", "HD-1");

        var rows = await Export(a.Client);

        Assert.Equal(["sku", "name", "description", "status", "slug", "featured", "categories"], Assert.Single(rows));
    }

    [Fact]
    public async Task A_dry_run_reports_creates_and_updates_and_saves_nothing()
    {
        var a = await TenantWithCatalog();

        var report = await Import(a.Client, "sku,name,Brand\nHD-1,Hammer drill 2,Bosch\nNEW-1,New product,\n", dryRun: true);

        Assert.Equal((1, 1, 0), (report.Created, report.Updated, report.Skipped));
        Assert.Equal([new ImportRow(1, "HD-1", "Update", []), new ImportRow(2, "NEW-1", "Create", [])], report.Rows);
        var list = await a.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products");
        Assert.Equal(["Circular saw", "Hammer drill"], list!.Items.Select(p => p.Name).Order());
    }

    [Fact]
    public async Task Invalid_rows_are_reported_with_row_and_column()
    {
        var a = await TenantWithCatalog();
        var csv = """
            sku,name,Power (W),Color,Sizes,categories,featured,Cordless,Released
            A-1,,1,Red,S,Tools,true,true,2026-01-01
            A-2,Ok,abc,Green,S|XL,Tools > Hand tools,maybe,yes,31/01/2026
            HD-1,,,,,,,,
            A-1,Duplicate,,,,,,,
            ,No sku,,,,,,,
            """;

        var report = await Import(a.Client, csv, dryRun: true);

        Assert.Equal((0, 0, 5), (report.Created, report.Updated, report.Skipped));
        Assert.Equal(["name"], report.Rows[0].Errors.Select(e => e.Column));
        Assert.Equal(new[] { "Power (W)", "Color", "Sizes", "categories", "featured", "Cordless", "Released" }.Order(),
            report.Rows[1].Errors.Select(e => e.Column).Order());
        Assert.Contains("'Green' is not an Option of Color.", report.Rows[1].Errors.Select(e => e.Message));
        Assert.Contains("Unknown Category 'Tools > Hand tools'.", report.Rows[1].Errors.Select(e => e.Message));
        Assert.Equal([new ImportError("name", "The name cannot be cleared.")], report.Rows[2].Errors);
        Assert.Equal("sku", Assert.Single(report.Rows[3].Errors).Column);
        Assert.Null(report.Rows[4].Sku);
        Assert.Equal("sku", Assert.Single(report.Rows[4].Errors).Column);
    }

    [Fact]
    public async Task Unknown_columns_are_warnings_and_status_and_slug_are_ignored()
    {
        var a = await TenantWithCatalog();

        var report = await Import(a.Client, "sku,name,status,slug,Weight\nNEW-1,Lamp,Published,my-lamp,3kg\n", dryRun: false);

        Assert.Equal(["Unknown column 'Weight' was ignored."], report.Warnings);
        var lamp = (await a.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products?q=Lamp"))!.Items.Single();
        Assert.Equal(("Draft", "lamp"), (lamp.Status, lamp.Slug));
    }

    public static TheoryData<string, byte[]> InvalidFiles => new()
    {
        { "empty", [] },
        { "too big", Encoding.UTF8.GetBytes("sku,name\n" + new string('x', 1024 * 1024)) },
        { "no sku header", Encoding.UTF8.GetBytes("name\nDrill\n") },
        { "too many rows", Encoding.UTF8.GetBytes("sku,name\n" + string.Concat(Enumerable.Range(1, 1001).Select(i => $"S-{i},P{i}\n"))) },
    };

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public async Task Invalid_files_return_400_on_file(string name, byte[] content)
    {
        var a = await CreateTenantWithAdmin("tenant-a");

        var response = await a.Client.PostAsync("/api/admin/products/import?dryRun=true", TestImages.Form(content, name, "text/csv"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["file"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task Round_trip_export_edit_import_changes_only_the_edited_cell()
    {
        var a = await TenantWithCatalog();
        var before = await Export(a.Client);
        var edited = before.Select(r => r.ToArray()).ToList();
        edited[2][7] = "Bosch"; // Brand of HD-1

        var report = await Import(a.Client, ToCsv(edited), dryRun: false);

        Assert.Equal((0, 2, 0), (report.Created, report.Updated, report.Skipped));
        var after = await Export(a.Client);
        Assert.Equal(edited, after);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_creates_nothing_the_second_time()
    {
        var a = await TenantWithCatalog();
        const string csv = "sku,name\nNEW-1,Lamp\nNEW-2,Lamp\n";

        var first = await Import(a.Client, csv, dryRun: false);
        var second = await Import(a.Client, csv, dryRun: false);

        Assert.Equal((2, 0), (first.Created, first.Updated));
        Assert.Equal((0, 2), (second.Created, second.Updated));
        var lamps = (await a.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products?q=Lamp"))!.Items;
        Assert.Equal(["lamp", "lamp-2"], lamps.Select(p => p.Slug).Order());
        Assert.All(lamps, p => Assert.Equal("Draft", p.Status));
    }

    [Fact]
    public async Task Empty_cells_clear_fields_and_missing_columns_leave_them_untouched()
    {
        var a = await TenantWithCatalog();

        await Import(a.Client, "sku,name,description,categories,Brand,Sizes\nHD-1,Hammer drill,,,,\n", dryRun: false);

        var rows = await Export(a.Client);
        var drill = rows.Single(r => r[0] == "HD-1");
        var header = rows[0].ToList();
        Assert.Equal("", drill[header.IndexOf("description")]);
        Assert.Equal("", drill[header.IndexOf("categories")]);
        Assert.Equal("", drill[header.IndexOf("Brand")]);
        Assert.Equal("", drill[header.IndexOf("Sizes")]);
        Assert.Equal("Red", drill[header.IndexOf("Color")]);
        Assert.Equal("true", drill[header.IndexOf("featured")]);
        Assert.Equal("Published", drill[header.IndexOf("status")]);
    }

    [Fact]
    public async Task A_file_with_valid_and_invalid_rows_applies_only_the_valid_ones()
    {
        var a = await TenantWithCatalog();

        var report = await Import(a.Client, "sku,name,Color,featured\nNEW-1,Lamp,Blue,true\nNEW-2,Chair,Green,\n", dryRun: false);

        Assert.Equal((1, 0, 1), (report.Created, report.Updated, report.Skipped));
        var names = (await a.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products"))!.Items.Select(p => p.Name);
        Assert.Contains("Lamp", names);
        Assert.DoesNotContain("Chair", names);
        var lamp = (await Export(a.Client)).Single(r => r[0] == "NEW-1");
        Assert.Equal(["NEW-1", "Lamp", "", "Draft", "lamp", "true", "", "", "Blue", "", "", "", ""], lamp);
    }

    [Fact]
    public async Task Isolation_SKUs_of_tenant_B_count_as_new_for_tenant_A()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        var b = await CreateTenantWithAdmin("tenant-b");
        await CreateProduct(b.Client, "Hammer drill", "HD-1");

        var report = await Import(a.Client, "sku,name\nHD-1,My drill\n", dryRun: false);

        Assert.Equal("Create", report.Rows[0].Outcome);
        Assert.Equal("Hammer drill", (await b.Client.GetFromJsonAsync<PagedBody<ProductListItem>>("/api/admin/products"))!.Items[0].Name);
    }

    private static string ToCsv(List<string[]> rows)
    {
        using var writer = new StringWriter();
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    csv.WriteField(cell);
                }

                csv.NextRecord();
            }
        }

        return writer.ToString();
    }

    private sealed record ImportError(string Column, string Message);

    private sealed record ImportRow(int Row, string? Sku, string Outcome, List<ImportError> Errors)
    {
        public bool Equals(ImportRow? other) =>
            other is not null && Row == other.Row && Sku == other.Sku && Outcome == other.Outcome && Errors.SequenceEqual(other.Errors);

        public override int GetHashCode() => HashCode.Combine(Row, Sku, Outcome);
    }

    private sealed record ImportReport(int Created, int Updated, int Skipped, List<ImportRow> Rows, List<string> Warnings);
}
