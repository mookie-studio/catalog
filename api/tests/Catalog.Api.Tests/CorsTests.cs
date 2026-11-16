using System.Net;

namespace Catalog.Api.Tests;

public sealed class CorsTests(CatalogApiFactory factory) : ApiTest(factory)
{
    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin/products");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    [Fact]
    public async Task Preflight_from_the_configured_web_origin_is_allowed()
    {
        var response = await Client.SendAsync(Preflight(CatalogApiFactory.AllowedOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(CatalogApiFactory.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Preflight_from_another_origin_is_not_allowed()
    {
        var response = await Client.SendAsync(Preflight("https://evil.test"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
