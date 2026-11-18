using System.Net;

namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Makes sure the demo Platform Admin and one Admin per Demo tenant exist with the configured passwords,
/// and that the Platform Admin is configured as one in the API.
/// </summary>
public static class AccountsStep
{
    public static async Task RunAsync(SeedContext context, CancellationToken ct)
    {
        var options = context.Options;
        context.PlatformAdminUserId = await context.Identity.EnsureUserAsync(options.PlatformAdminEmail!, options.PlatformAdminPassword!, ct);
        foreach (var tenant in context.Dataset.Tenants)
        {
            var state = new TenantState(tenant)
            {
                AdminUserId = await context.Identity.EnsureUserAsync(tenant.Tenant.AdminEmail, options.AdminPassword!, ct),
            };
            context.Tenants.Add(state);
        }

        var token = await context.Identity.SignInAsync(options.PlatformAdminEmail!, options.PlatformAdminPassword!, ct);
        context.Platform = context.Api(token);
        try
        {
            await context.Platform.GetAsync<Paged<TenantResponse>>("/api/platform/tenants?pageSize=1", ct);
        }
        catch (ApiException e) when (e.Status is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            throw new SeedException(
                $"The demo Platform Admin (user id {context.PlatformAdminUserId}) is not a Platform Admin. " +
                "Add it to Auth:PlatformAdminUserIds (Auth__PlatformAdminUserIds__<n> on App Service) and restart the API.");
        }

        context.Log.Info($"Accounts ready: Platform Admin {options.PlatformAdminEmail} and {context.Tenants.Count} demo Admins.");
    }
}
