using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Tests;

public sealed class InquiryRateLimitTests(CatalogApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task The_third_Inquiry_from_one_IP_in_a_window_gets_429_while_other_IPs_and_endpoints_continue()
    {
        var a = await CreateTenantWithAdmin("tenant-a");
        await a.Client.PutAsJsonAsync("/api/admin/tenant/profile", new { inquiriesEnabled = true });
        await CreatePublishedProduct(a.Client, "Hammer drill", "HD-1");
        await using var limited = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimits:Inquiries:PermitLimit", "2"));
        var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await Send(client, "203.0.113.1")).StatusCode);
        }

        Assert.Equal([HttpStatusCode.Accepted, HttpStatusCode.Accepted, HttpStatusCode.TooManyRequests], statuses);
        var rejected = await Send(client, "203.0.113.1");
        Assert.True(rejected.Headers.Contains("Retry-After"));
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Too many requests", problem!.Title);

        Assert.Equal(HttpStatusCode.Accepted, (await Send(client, "198.51.100.7")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/public/tenant-a")).StatusCode);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string ip)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/tenant-a/products/hammer-drill/inquiries")
        {
            Content = JsonContent.Create(new { name = "Ana", email = "ana@example.com", message = "Hello" }),
        };
        request.Headers.Add("X-Forwarded-For", ip);
        return client.SendAsync(request);
    }
}
