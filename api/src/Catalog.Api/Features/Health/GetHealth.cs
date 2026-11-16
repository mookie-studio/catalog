using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Catalog.Api.Features.Health;

public static class GetHealth
{
    public sealed record Response(string Status);

    public static IEndpointRouteBuilder MapGetHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", Handle)
            .WithName("GetHealth")
            .WithTags("Health");

        return app;
    }

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        CatalogDbContext db, CancellationToken ct)
    {
        bool canConnect;
        try
        {
            canConnect = await db.Database.CanConnectAsync(ct);
        }
        catch (Exception)
        {
            canConnect = false;
        }

        if (!canConnect)
        {
            return TypedResults.Problem(
                title: "Service unavailable",
                detail: "The database is unreachable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.Ok(new Response("ok"));
    }
}
