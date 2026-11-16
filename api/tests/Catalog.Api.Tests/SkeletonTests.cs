using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class SkeletonTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await Client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.Equal("ok", body?.Status);
    }

    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/api/admin/does-not-exist")]
    [InlineData("/api/platform/does-not-exist")]
    [InlineData("/api/public/some-tenant/does-not-exist")]
    public async Task Unknown_route_returns_problem_details_404(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem?.Status);
    }

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await Client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", document);
        Assert.Contains("/api/health", document);
    }

    private sealed record HealthBody(string Status);
}
