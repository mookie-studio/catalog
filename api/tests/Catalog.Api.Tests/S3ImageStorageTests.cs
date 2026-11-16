using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Catalog.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Testcontainers.Minio;

namespace Catalog.Api.Tests;

/// <summary>
/// Verifies the S3-compatible implementation against MinIO. Image storage is a fake in the HTTP
/// tests, so its real implementation is checked here at its storage boundary.
/// </summary>
public sealed class S3ImageStorageTests
{
    [SkippableFact]
    public async Task Saves_serves_from_the_public_URL_and_deletes()
    {
        Skip.If(
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CATALOG_TEST_DB")) && !DockerIsAvailable(),
            "Docker is unavailable and CATALOG_TEST_DB is used.");

        await using var minio = new MinioBuilder("minio/minio:RELEASE.2023-01-31T02-24-19Z").Build();
        await minio.StartAsync();
        const string bucket = "catalog-images";
        await CreatePublicBucket(minio, bucket);
        var options = new StorageOptions
        {
            ServiceUrl = minio.GetConnectionString(),
            Bucket = bucket,
            AccessKey = minio.GetAccessKey(),
            SecretKey = minio.GetSecretKey(),
            PublicBaseUrl = $"{minio.GetConnectionString().TrimEnd('/')}/{bucket}",
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

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(url)).StatusCode);
    }

    private static async Task CreatePublicBucket(MinioContainer minio, string bucket)
    {
        using var client = new AmazonS3Client(
            new BasicAWSCredentials(minio.GetAccessKey(), minio.GetSecretKey()),
            new AmazonS3Config { ServiceURL = minio.GetConnectionString(), ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await client.PutBucketAsync(bucket);
        await client.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = bucket,
            Policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},
                "Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{bucket}}/*"]}]}
                """,
        });
    }

    private static bool DockerIsAvailable() =>
        File.Exists("/var/run/docker.sock") || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"));
}
