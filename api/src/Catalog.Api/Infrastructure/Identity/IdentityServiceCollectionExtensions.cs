namespace Catalog.Api.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogIdentityProvider(this IServiceCollection services)
    {
        services.AddOptions<SupabaseOptions>().BindConfiguration(SupabaseOptions.SectionName);
        services.AddHttpClient<IIdentityProvider, SupabaseIdentityProvider>();
        return services;
    }
}
