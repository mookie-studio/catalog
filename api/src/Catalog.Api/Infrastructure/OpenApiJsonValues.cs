using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Catalog.Api.Infrastructure;

/// <summary>
/// Maps of free JSON values (Dictionary&lt;string, JsonElement&gt;, such as Product attribute values) get
/// "additionalProperties: {}" so generated clients accept any value instead of none.
/// </summary>
public static class OpenApiJsonValues
{
    public static void Configure(OpenApiOptions options) =>
        options.AddSchemaTransformer((schema, context, _) =>
        {
            var type = context.JsonTypeInfo.Type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                type.GetGenericArguments()[1] == typeof(JsonElement))
            {
                schema.AdditionalProperties = new OpenApiSchema();
            }

            return Task.CompletedTask;
        });
}
