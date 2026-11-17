using System.Collections.Concurrent;
using Catalog.Api.Infrastructure.Storage;

namespace Catalog.Api.Tests;

/// <summary>Keeps Images in memory; public URLs are https://images.test/{key}.</summary>
public sealed class InMemoryImageStorage : IImageStorage
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _files = new();

    public IReadOnlyDictionary<string, (byte[] Content, string ContentType)> Files => _files;

    /// <summary>When set, saving a key that matches throws, to simulate a storage failure.</summary>
    public Func<string, bool>? FailSaving { get; set; }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        if (FailSaving?.Invoke(key) == true)
        {
            throw new IOException($"Simulated storage failure for {key}.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        _files[key] = (buffer.ToArray(), contentType);
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        _files.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key) => $"https://images.test/{key}";

    public static string KeyOf(string url) => url["https://images.test/".Length..];

    public void Reset()
    {
        _files.Clear();
        FailSaving = null;
    }
}
