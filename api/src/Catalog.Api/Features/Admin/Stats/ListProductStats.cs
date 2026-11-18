using System.Globalization;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Stats;

/// <summary>
/// Product views and Inquiries per product over a range of UTC days (inclusive), most viewed first.
/// Only products with at least one view or Inquiry in the range are listed.
/// </summary>
public static class ListProductStats
{
    public const int MaxRangeDays = 366;
    public const int DefaultRangeDays = 30;

    public sealed record Item(Guid ProductId, string Name, string Slug, ProductStatus Status, int Views, int Inquiries);

    public static RouteGroupBuilder MapListProductStats(this RouteGroupBuilder admin)
    {
        admin.MapGet("/stats/products", Handle).WithName("ListProductStats");
        return admin;
    }

    private static async Task<Results<Ok<PagedResponse<Item>>, ValidationProblem>> Handle(
        string? from, string? to, int? page, int? pageSize, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        var today = DateOnly.FromDateTime(Timestamps.UtcNow().UtcDateTime);
        var toDay = ParseDay(to, "to", today, errors);
        var fromDay = ParseDay(from, "from", toDay.AddDays(1 - DefaultRangeDays), errors);
        if (!errors.HasErrors && fromDay > toDay)
        {
            errors.Add("from", "The start date must not be after the end date.");
        }
        else if (!errors.HasErrors && toDay.DayNumber - fromDay.DayNumber + 1 > MaxRangeDays)
        {
            errors.Add("from", $"The range cannot be longer than {MaxRangeDays} days.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var paging = Paging.From(page, pageSize);
        var start = new DateTimeOffset(fromDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(toDay.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var views = db.ProductViewCounts
            .Where(v => v.Day >= fromDay && v.Day <= toDay)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, Views = g.Sum(v => v.Count) });
        var inquiries = db.Inquiries
            .Where(i => i.CreatedAt >= start && i.CreatedAt < end)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Inquiries = g.Count() });

        var query = db.Products
            .Select(p => new
            {
                Product = p,
                Views = views.Where(v => v.ProductId == p.Id).Select(v => v.Views).FirstOrDefault(),
                Inquiries = inquiries.Where(i => i.ProductId == p.Id).Select(i => i.Inquiries).FirstOrDefault(),
            })
            .Where(x => x.Views > 0 || x.Inquiries > 0);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.Views).ThenByDescending(x => x.Inquiries)
            .ThenBy(x => x.Product.Name).ThenBy(x => x.Product.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new Item(x.Product.Id, x.Product.Name, x.Product.Slug, x.Product.Status, x.Views, x.Inquiries))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<Item>(items, paging.Page, paging.PageSize, totalCount));
    }

    private static DateOnly ParseDay(string? value, string field, DateOnly fallback, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return day;
        }

        errors.Add(field, "The date must use the format yyyy-MM-dd.");
        return fallback;
    }
}
