namespace Catalog.Api.Features.Health;

public static class GetHealth
{
    public sealed record Response(string Status);

    public static IEndpointRouteBuilder MapGetHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => TypedResults.Ok(new Response("ok")))
            .WithName("GetHealth")
            .WithTags("Health");

        return app;
    }
}
