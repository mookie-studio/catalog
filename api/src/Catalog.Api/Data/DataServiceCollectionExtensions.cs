using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public static class DataServiceCollectionExtensions
{
    public const string ConnectionStringName = "Catalog";

    public static IServiceCollection AddCatalogData(this IServiceCollection services)
    {
        // The connection string is resolved lazily so tests can override it.
        services.AddScoped<ICurrentTenant, CurrentTenant>();

        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            options.UseNpgsql(configuration.GetConnectionString(ConnectionStringName));
        });

        return services;
    }
}
