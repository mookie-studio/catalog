using Microsoft.AspNetCore.OpenApi;

namespace Catalog.Api.Infrastructure;

/// <summary>
/// Slices declare nested records such as GetMe.Response; schema ids are prefixed with the declaring
/// slice (GetMeResponse) so they are unique and readable in the generated web client.
/// </summary>
public static class OpenApiSchemaNames
{
    public static void Configure(OpenApiOptions options) =>
        options.CreateSchemaReferenceId = info =>
            OpenApiOptions.CreateDefaultSchemaReferenceId(info) is null ? null : NameOf(info.Type);

    private static string NameOf(Type type)
    {
        var name = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
        if (type.IsGenericType)
        {
            name += "Of" + string.Concat(type.GetGenericArguments().Select(NameOf));
        }

        return type.IsNested && type.DeclaringType is { } declaring ? NameOf(declaring) + name : name;
    }
}
