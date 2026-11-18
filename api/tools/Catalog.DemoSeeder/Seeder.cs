using Catalog.DemoSeeder.Steps;

namespace Catalog.DemoSeeder;

/// <summary>A run cannot continue; the message says why and what to do.</summary>
public sealed class SeedException(string message) : Exception(message);

/// <summary>
/// The Demo seeder (ADR 0014): makes every Demo tenant match the Demo dataset through the Catalog API.
/// Running it again converges, so it is also how the demo is reset.
/// </summary>
public static class Seeder
{
    public static async Task RunAsync(SeedContext context, CancellationToken ct)
    {
        var log = context.Log;
        await AccountsStep.RunAsync(context, ct);
        await TenantsStep.RunAsync(context, ct);

        foreach (var state in context.Tenants)
        {
            log.Info($"{state.Slug}:");
            await ProfileStep.RunAsync(context, state, ct);
            await CatalogStep.RunAsync(context, state, ct);
            await ProductsStep.RunAsync(context, state, ct);
            await ImagesStep.RunAsync(context, state, ct);
            if (!context.Arguments.SkipActivity)
            {
                await ActivityStep.RunAsync(context, state, ct);
            }
        }

        await DeactivateStep.RunAsync(context, ct);
        log.Summary();
    }
}
