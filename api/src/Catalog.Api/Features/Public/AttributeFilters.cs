using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// Visitor filters on Filterable attributes, from repeated attr.{definitionId}=value query parameters.
/// Values of one definition are OR-ed; different definitions are AND-ed (ADR 0006).
/// </summary>
public static class AttributeFilters
{
    public const string Prefix = "attr.";

    public sealed record Filter(AttributeDefinition Definition, IReadOnlyList<string> Values);

    /// <summary>Parses the attr.* parameters; unknown, non-filterable or malformed ones are validation errors.</summary>
    public static async Task<List<Filter>> ParseAsync(IQueryCollection query, CatalogDbContext db, ValidationErrors errors, CancellationToken ct)
    {
        var requested = query
            .Where(q => q.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (requested.Count == 0)
        {
            return [];
        }

        var definitions = await db.AttributeDefinitions.Include(d => d.Options).AsNoTracking().ToDictionaryAsync(d => d.Id, ct);
        var filters = new List<Filter>();
        foreach (var (key, raw) in requested)
        {
            if (!Guid.TryParse(key[Prefix.Length..], out var id) || !definitions.TryGetValue(id, out var definition) || !definition.IsFilterable)
            {
                errors.Add(key, "Unknown or non-filterable attribute.");
                continue;
            }

            var values = raw.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).Distinct().ToList();
            var invalid = values.FirstOrDefault(v => !IsValid(definition, v));
            if (invalid is not null)
            {
                errors.Add(key, $"'{invalid}' is not a valid value for this attribute.");
                continue;
            }

            if (values.Count > 0)
            {
                filters.Add(new Filter(definition, values));
            }
        }

        return filters;
    }

    private static bool IsValid(AttributeDefinition definition, string value) => definition.Type switch
    {
        AttributeType.OptionList => Guid.TryParse(value, out var option) && definition.Options.Any(o => o.Id == option),
        AttributeType.Number => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        AttributeType.Boolean => value is "true" or "false",
        AttributeType.Date => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        _ => true,
    };

    public static IQueryable<Product> Apply(IQueryable<Product> products, IEnumerable<Filter> filters)
    {
        foreach (var filter in filters)
        {
            var product = Expression.Parameter(typeof(Product), "p");
            var body = filter.Values
                .Select(value => Match(product, filter.Definition, value))
                .Aggregate(Expression.OrElse);
            products = products.Where(Expression.Lambda<Func<Product, bool>>(body, product));
        }

        return products;
    }

    /// <summary>One value's condition: jsonb containment, or a case-insensitive comparison for Text.</summary>
    private static Expression Match(ParameterExpression product, AttributeDefinition definition, string value)
    {
        var attributes = Expression.Property(product, nameof(Product.Attributes));
        var key = definition.Id.ToString();

        if (definition.Type == AttributeType.Text)
        {
            Expression<Func<string, string, string, bool>> textEquals = (json, k, v) =>
                CatalogDbContext.JsonbExtractPathText(json, k)!.ToLower() == v;
            return Inline(textEquals, attributes, Parameter(key), Parameter(value.ToLowerInvariant()));
        }

        object? jsonValue = definition.Type switch
        {
            AttributeType.OptionList when definition.AllowsMultiple => new[] { value },
            AttributeType.Number => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture),
            AttributeType.Boolean => value == "true",
            _ => value,
        };
        var contained = JsonSerializer.Serialize(new Dictionary<string, object?> { [key] = jsonValue });
        Expression<Func<string, string, bool>> contains = (json, c) => EF.Functions.JsonContains(json, c);
        return Inline(contains, attributes, Parameter(contained));
    }

    /// <summary>Wraps a value so EF Core sends it as a SQL parameter rather than inlining it.</summary>
    private static Expression Parameter(string value) =>
        Expression.Property(Expression.Constant(new Holder(value)), nameof(Holder.Value));

    private static Expression Inline(LambdaExpression lambda, params Expression[] arguments) =>
        new ParameterReplacer(lambda.Parameters, arguments).Visit(lambda.Body);

    private sealed record Holder(string Value);

    private sealed class ParameterReplacer(IReadOnlyList<ParameterExpression> from, IReadOnlyList<Expression> to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            var index = from.ToList().IndexOf(node);
            return index >= 0 ? to[index] : base.VisitParameter(node);
        }
    }
}
