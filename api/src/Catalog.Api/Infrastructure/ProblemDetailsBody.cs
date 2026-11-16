using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Infrastructure;

/// <summary>Helpers to build ProblemDetails bodies for typed results such as Conflict.</summary>
public static class ProblemDetailsBody
{
    public static ProblemDetails Conflict(string detail) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict",
        Detail = detail,
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
    };
}
