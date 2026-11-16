using Catalog.Api.Data;
using Catalog.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Catalog.Api.Tests;

/// <summary>
/// Hosts the API against a real PostgreSQL: a Testcontainers instance, or the
/// connection string in the CATALOG_TEST_DB environment variable when it is set.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseVariable = "CATALOG_TEST_DB";

    private PostgreSqlContainer? _container;
    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = "";

    public FakeIdentityProvider IdentityProvider { get; } = new();

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(TestDatabaseVariable);
        if (!string.IsNullOrWhiteSpace(external))
        {
            ConnectionString = external;
        }
        else
        {
            _container = new PostgreSqlBuilder("postgres:17").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>Deletes all data so each test starts from an empty database.</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
        IdentityProvider.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DataServiceCollectionExtensions.ConnectionStringName}", ConnectionString);
        builder.UseSetting("Auth:Issuer", TestTokens.Issuer);
        builder.UseSetting("Auth:Audience", TestTokens.Audience);
        builder.UseSetting("Auth:JwtSecret", TestTokens.Secret);
        builder.UseSetting("Auth:PlatformAdminUserIds:0", TestTokens.PlatformAdminUserId.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIdentityProvider>();
            services.AddSingleton<IIdentityProvider>(IdentityProvider);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class CatalogApiCollection : ICollectionFixture<CatalogApiFactory>
{
    public const string Name = "Catalog API";
}

/// <summary>
/// Base class for HTTP-level tests: every test starts from an empty database.
/// </summary>
[Collection(CatalogApiCollection.Name)]
public abstract class ApiTest(CatalogApiFactory factory) : IAsyncLifetime
{
    protected CatalogApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    /// <summary>A client authenticated as the given user.</summary>
    protected HttpClient ClientFor(Guid userId, string email)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestTokens.Create(userId, email));
        return client;
    }

    /// <summary>A client authenticated as the configured Platform Admin.</summary>
    protected HttpClient PlatformAdminClient() => ClientFor(TestTokens.PlatformAdminUserId, "platform@catalog.test");

    /// <summary>Creates a Tenant through the platform API.</summary>
    protected static async Task<TenantBody> CreateTenant(HttpClient platformClient, string slug, string? name = null)
    {
        var response = await platformClient.PostAsJsonAsync("/api/platform/tenants", new { name = name ?? slug, slug });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantBody>())!;
    }

    /// <summary>Creates a Tenant and invites an Admin to it; returns a client authenticated as that Admin.</summary>
    protected async Task<AdminSession> CreateTenantWithAdmin(string slug, string? email = null)
    {
        var platform = PlatformAdminClient();
        var tenant = await CreateTenant(platform, slug);
        var adminEmail = email ?? $"admin@{slug}.test";
        var response = await platform.PostAsJsonAsync($"/api/platform/tenants/{tenant.Id}/admins", new { email = adminEmail });
        response.EnsureSuccessStatusCode();
        var admin = (await response.Content.ReadFromJsonAsync<AdminBody>())!;
        return new AdminSession(tenant, admin, ClientFor(admin.UserId, admin.Email));
    }

    /// <summary>Creates a Product as the given Admin.</summary>
    protected static async Task<ProductBody> CreateProduct(
        HttpClient adminClient, string name, string sku, string? description = null)
    {
        var response = await adminClient.PostAsJsonAsync("/api/admin/products", new { name, sku, description });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductBody>())!;
    }

    /// <summary>A client authenticated as someone who is not a Platform Admin.</summary>
    protected HttpClient OutsiderClient() => ClientFor(Guid.CreateVersion7(), "outsider@catalog.test");

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
