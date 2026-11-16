namespace Catalog.Api.Infrastructure.Storage;

/// <summary>Where Product Images are stored and served from (ADR 0005).</summary>
public interface IImageStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);

    string GetPublicUrl(string key);
}
