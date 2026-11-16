using System.Net;
using Catalog.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Api.Tests;

public sealed class DatabaseTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Health_returns_503_when_the_database_is_unreachable()
    {
        using var unreachable = Factory.WithWebHostBuilder(builder => builder.UseSetting(
            $"ConnectionStrings:{DataServiceCollectionExtensions.ConnectionStringName}",
            "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2"));
        var client = unreachable.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(503, problem?.Status);
    }

    // The two tests below share a slug: whichever runs second would fail on the
    // unique index if data leaked between tests.
    [Fact]
    public Task Data_written_by_a_test_is_not_visible_to_the_next_one_A() => InsertTenantWithFixedSlug();

    [Fact]
    public Task Data_written_by_a_test_is_not_visible_to_the_next_one_B() => InsertTenantWithFixedSlug();

    private async Task InsertTenantWithFixedSlug()
    {
        // Seeding goes through the DbContext until the platform endpoints exist;
        // assertions still go through HTTP-visible behavior where possible.
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Tenants.Add(new Tenant { Name = "Isolation", Slug = "isolation" });
        await db.SaveChangesAsync();
    }
}
