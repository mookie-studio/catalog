using System.Net;

namespace Catalog.Api.Tests;

public sealed class HealthEndpointsTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_and_aliveness_answer_200_without_signing_in(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
