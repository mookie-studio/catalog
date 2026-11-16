namespace Catalog.Api.Infrastructure;

/// <summary>
/// The SPA is served from another origin in production (Cloudflare Pages), so browsers need CORS.
/// Allowed origins come from the "Cors:AllowedOrigins" list; with none configured, no origin is allowed.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    public const string SectionName = "Cors";

    public static IServiceCollection AddCatalogCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection($"{SectionName}:AllowedOrigins").Get<string[]>() ?? [];
        return services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }
}
