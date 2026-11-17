using SkiaSharp;

namespace Catalog.Api.Tests;

/// <summary>Real, decodable images (so Thumbnails can be generated), plus files that only look like images.</summary>
public static class TestImages
{
    public static byte[] Png(int width = 64, int height = 48) => Encode(SKEncodedImageFormat.Png, width, height);

    public static byte[] Jpeg(int width = 64, int height = 48) => Encode(SKEncodedImageFormat.Jpeg, width, height);

    public static byte[] WebP(int width = 64, int height = 48) => Encode(SKEncodedImageFormat.Webp, width, height);

    /// <summary>A file with GIF magic bytes: a format Catalog does not accept.</summary>
    public static byte[] Gif() => Pad("GIF89a"u8.ToArray(), 64);

    /// <summary>A file of <paramref name="size"/> bytes with PNG magic bytes; size checks happen before decoding.</summary>
    public static byte[] OversizedPng(int size) => Pad([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], size);

    /// <summary>A file with valid PNG magic bytes whose content cannot be decoded.</summary>
    public static byte[] CorruptPng() => Pad([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], 256);

    /// <summary>Width and height of an encoded image.</summary>
    public static (int Width, int Height) SizeOf(byte[] image)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(image)) ?? throw new InvalidOperationException("Not an image.");
        return (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>Format of an encoded image.</summary>
    public static SKEncodedImageFormat FormatOf(byte[] image)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(image)) ?? throw new InvalidOperationException("Not an image.");
        return codec.EncodedFormat;
    }

    private static byte[] Encode(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SteelBlue);
            using var paint = new SKPaint { Color = SKColors.Orange };
            canvas.DrawRect(0, 0, width / 2f, height / 2f, paint);
        }

        using var data = bitmap.Encode(format, 90);
        return data.ToArray();
    }

    private static byte[] Pad(byte[] header, int size)
    {
        var bytes = new byte[Math.Max(size, header.Length)];
        header.CopyTo(bytes, 0);
        return bytes;
    }

    public static MultipartFormDataContent Form(byte[] content, string fileName, string contentType = "application/octet-stream")
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}
