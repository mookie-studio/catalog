using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Catalog.Api.Infrastructure.Storage;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;

namespace Catalog.Api.Tests;

/// <summary>
/// Verifies the S3-compatible implementation against a real S3 API (Adobe S3Mock, since the MinIO
/// images are no longer published on Docker Hub). Image storage is a fake in the HTTP tests, so its
/// real implementation is checked here at its storage boundary.
/// </summary>
public sealed class S3ImageStorageTests
{
    private const int S3Port = 9090;

    [SkippableFact]
    public async Task Saves_serves_from_the_public_URL_and_deletes()
    {
        Skip.If(
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CATALOG_TEST_DB")) && !DockerIsAvailable(),
            "Docker is unavailable and CATALOG_TEST_DB is used.");

        await using var s3 = new ContainerBuilder("adobe/s3mock:3.12.0")
            .WithPortBinding(S3Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(S3Port).ForPath("/")))
            .Build();
        await s3.StartAsync();
        var serviceUrl = $"http://{s3.Hostname}:{s3.GetMappedPublicPort(S3Port)}";
        const string bucket = "catalog-images";
        await CreateBucket(serviceUrl, bucket);
        var options = new StorageOptions
        {
            ServiceUrl = serviceUrl,
            Bucket = bucket,
            AccessKey = "test",
            SecretKey = "test",
            PublicBaseUrl = $"{serviceUrl}/{bucket}",
        };
        using var storage = new S3ImageStorage(Options.Create(options));
        using var http = new HttpClient();
        var key = $"{Guid.CreateVersion7()}/{Guid.CreateVersion7()}/image.png";
        var content = TestImages.Png();

        await storage.SaveAsync(key, new MemoryStream(content), "image/png", CancellationToken.None);
        var url = storage.GetPublicUrl(key);
        var served = await http.GetAsync(url);

        Assert.Equal($"{options.PublicBaseUrl}/{key}", url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(content, await served.Content.ReadAsByteArrayAsync());

        await storage.DeleteAsync(key, CancellationToken.None);

        // Stores without public listing may answer 403 instead of 404 for a missing key.
        Assert.Contains((await http.GetAsync(url)).StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
    }

    private static async Task CreateBucket(string serviceUrl, string bucket)
    {
        using var client = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config { ServiceURL = serviceUrl, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await client.PutBucketAsync(bucket);
    }

    private static bool DockerIsAvailable() =>
        File.Exists("/var/run/docker.sock") || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));
}
