using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Features.Admin.Attributes;

public static class AttributeRules
{
    public static void ValidateName(string? name, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "The name is required.");
        }
        else if (name.Trim().Length > AttributeDefinition.NameMaxLength)
        {
            errors.Add("name", $"The name must be at most {AttributeDefinition.NameMaxLength} characters.");
        }
    }

    public static Conflict<ProblemDetails> NameInUse() =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict("An Attribute Definition with this name already exists in this Tenant."));
}
