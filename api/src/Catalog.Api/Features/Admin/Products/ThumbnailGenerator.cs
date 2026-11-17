using SkiaSharp;

namespace Catalog.Api.Features.Admin.Products;

/// <summary>Builds the Thumbnail of an Image (ADR 0008): at most 480 px wide, never upscaled, WebP quality 80.</summary>
public static class ThumbnailGenerator
{
    public const int MaxWidth = 480;
    public const int Quality = 80;
    public const string ContentType = "image/webp";

    /// <summary>Returns the WebP bytes of the Thumbnail, or null when the image cannot be decoded.</summary>
    public static byte[]? Generate(ReadOnlySpan<byte> image)
    {
        using var data = SKData.CreateCopy(image);
        using var original = SKBitmap.Decode(data);
        if (original is null || original.Width <= 0 || original.Height <= 0)
        {
            return null;
        }

        var width = Math.Min(original.Width, MaxWidth);
        var height = Math.Max(1, (int)Math.Round(original.Height * (double)width / original.Width));
        using var resized = width == original.Width
            ? original.Copy()
            : original.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (resized is null)
        {
            return null;
        }

        using var encoded = resized.Encode(SKEncodedImageFormat.Webp, Quality);
        return encoded?.ToArray();
    }

    /// <summary>Storage key of an Image's Thumbnail, next to the original.</summary>
    public static string KeyFor(Guid tenantId, Guid productId, Guid imageId) => $"{tenantId}/{productId}/{imageId}-thumb.webp";
}
