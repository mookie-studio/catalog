using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Catalog.Api.Infrastructure;

/// <summary>
/// Route group parameters such as {tenantSlug} are read by endpoint filters, not by handlers, so the
/// generator does not list them. This adds every route parameter missing from an operation.
/// </summary>
public static partial class OpenApiRouteParameters
{
    public static void Configure(OpenApiOptions options) =>
        options.AddOperationTransformer((operation, context, _) =>
        {
            var template = context.Description.RelativePath ?? string.Empty;
            foreach (Match match in RouteParameter().Matches(template))
            {
                var name = match.Groups[1].Value;
                operation.Parameters ??= [];
                if (operation.Parameters.Any(p => p.In == ParameterLocation.Path && p.Name == name))
                {
                    continue;
                }

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                });
            }

            return Task.CompletedTask;
        });

    [GeneratedRegex(@"\{([A-Za-z0-9_]+)(?::[^}]*)?\}")]
    private static partial Regex RouteParameter();
}
