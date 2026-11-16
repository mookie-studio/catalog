using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Infrastructure;

/// <summary>Helpers to build ProblemDetails bodies for typed results such as Conflict.</summary>
public static class ProblemDetailsBody
{
    /// <summary>
    /// A 409 body. When the conflict is about one request field, it also carries an <c>errors</c> map shaped
    /// like a validation problem, so clients can show the message next to that field.
    /// </summary>
    public static ProblemDetails Conflict(string detail, string? field = null)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = detail,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        };
        if (field is not null)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = [detail] };
        }

        return problem;
    }
}
