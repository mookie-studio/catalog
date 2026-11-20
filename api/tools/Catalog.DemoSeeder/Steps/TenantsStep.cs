using Npgsql;

namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Creates or reactivates every Demo tenant (content is written while active; see <see cref="DeactivateStep"/>),
/// after checking that each existing match really is a Demo tenant. Then links each demo Admin to its Tenant.
/// </summary>
public static class TenantsStep
{
    public static async Task RunAsync(SeedContext context, CancellationToken ct)
    {
        var existing = (await context.Platform.GetAllPagesAsync<TenantResponse>("/api/platform/tenants", ct))
            .ToDictionary(t => t.Slug, StringComparer.Ordinal);

        // Guard first, for every Demo tenant, so nothing is changed when one fails.
        foreach (var state in context.Tenants)
        {
            if (!existing.TryGetValue(state.Slug, out var tenant))
            {
                continue;
            }

            var admins = await context.Platform.GetAllPagesAsync<AdminResponse>($"/api/platform/tenants/{tenant.Id}/admins", ct);
            if (admins.Count > 0 && !admins.Any(a => string.Equals(a.Email, state.Demo.Tenant.AdminEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new SeedException($"Tenant '{state.Slug}' exists and is not a Demo tenant; nothing was changed.");
            }

            state.Existing = tenant;
        }

        foreach (var state in context.Tenants)
        {
            if (state.Existing is not { } tenant)
            {
                TenantResponse created;
                try
                {
                    created = await context.Platform.PostAsync<TenantResponse>(
                        "/api/platform/tenants", new { name = state.Demo.Tenant.Name, slug = state.Slug }, ct);
                }
                catch (ApiException e) when (e.Status == System.Net.HttpStatusCode.Conflict)
                {
                    // The slug is an Old slug of another Tenant (ADR 0009): most likely this Demo tenant was renamed.
                    throw new SeedException(
                        $"The Tenant slug '{state.Slug}' is reserved by another Tenant (an Old slug). If a Demo tenant was renamed " +
                        "during a demo, change its slug back to the dataset one in the Platform Admin area, then run the seeder again.");
                }

                state.Id = created.Id;
                context.Log.Change(state.Slug, "tenant created", state.Demo.Tenant.Name);
                continue;
            }

            state.Id = tenant.Id;
            if (tenant.Name != state.Demo.Tenant.Name)
            {
                context.Log.Info($"  {state.Slug}: the Tenant name is '{tenant.Name}', not '{state.Demo.Tenant.Name}'; the API cannot rename Tenants, so it is left as it is.");
            }

            if (!tenant.IsActive)
            {
                await context.Platform.PostAsync($"/api/platform/tenants/{tenant.Id}/reactivate", null, ct);
                if (state.Demo.Tenant.IsActive)
                {
                    context.Log.Change(state.Slug, "tenant reactivated", state.Slug);
                }
            }
        }

        await LinkAdminsAsync(context, ct);
    }

    /// <summary>
    /// Inserts the AdminMembership of each demo Admin (ADR 0014: the API only adds Admins by invitation email),
    /// then signs in as that Admin and checks the API sees the right Tenant.
    /// </summary>
    private static async Task LinkAdminsAsync(SeedContext context, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(context.Options.DatabaseConnectionString);
        await connection.OpenAsync(ct);
        foreach (var state in context.Tenants)
        {
            var email = state.Demo.Tenant.AdminEmail.Trim().ToLowerInvariant();
            await using (var find = new NpgsqlCommand("""SELECT "TenantId" FROM "AdminMemberships" WHERE "UserId" = @user""", connection))
            {
                find.Parameters.AddWithValue("user", state.AdminUserId);
                var tenantId = await find.ExecuteScalarAsync(ct);
                if (tenantId is Guid other && other != state.Id)
                {
                    throw new SeedException($"{email} is already an Admin of another Tenant; remove it there first.");
                }

                if (tenantId is null)
                {
                    await using var insert = new NpgsqlCommand("""
                        INSERT INTO "AdminMemberships" ("Id", "TenantId", "UserId", "Email", "CreatedAt")
                        VALUES (@id, @tenant, @user, @email, @now)
                        ON CONFLICT DO NOTHING
                        """, connection);
                    insert.Parameters.AddWithValue("id", Guid.CreateVersion7());
                    insert.Parameters.AddWithValue("tenant", state.Id);
                    insert.Parameters.AddWithValue("user", state.AdminUserId);
                    insert.Parameters.AddWithValue("email", email);
                    insert.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
                    if (await insert.ExecuteNonQueryAsync(ct) == 0)
                    {
                        // The membership exists for an older user with the same email (the account was deleted
                        // and created again in the identity provider): point it at the current user.
                        await using var relink = new NpgsqlCommand("""
                            UPDATE "AdminMemberships" SET "UserId" = @user WHERE "TenantId" = @tenant AND "Email" = @email
                            """, connection);
                        relink.Parameters.AddWithValue("tenant", state.Id);
                        relink.Parameters.AddWithValue("user", state.AdminUserId);
                        relink.Parameters.AddWithValue("email", email);
                        await relink.ExecuteNonQueryAsync(ct);
                    }

                    context.Log.Change(state.Slug, "admin linked", email);
                }
            }

            var token = await context.Identity.SignInAsync(email, context.Options.AdminPassword!, ct);
            state.Admin = context.Api(token);
            var current = await state.Admin.GetAsync<CurrentTenantResponse>("/api/admin/tenant", ct);
            if (current.Slug != state.Slug)
            {
                throw new SeedException($"{email} signs in to '{current.Slug}', not '{state.Slug}'.");
            }
        }
    }
}

/// <summary>Deactivates, at the very end, every Demo tenant the dataset marks as not active.</summary>
public static class DeactivateStep
{
    public static async Task RunAsync(SeedContext context, CancellationToken ct)
    {
        foreach (var state in context.Tenants.Where(t => !t.Demo.Tenant.IsActive))
        {
            await context.Platform.PostAsync($"/api/platform/tenants/{state.Id}/deactivate", null, ct);
            if (state.Existing is not { IsActive: false })
            {
                context.Log.Change(state.Slug, "tenant deactivated", state.Slug);
            }
        }
    }
}
