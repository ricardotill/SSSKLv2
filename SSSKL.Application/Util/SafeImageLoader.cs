using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace SSSKL.Application.Util;

/// <summary>
/// Loads untrusted images with a pixel budget. A small, highly compressed file can declare huge
/// dimensions and exhaust memory when decoded, so the header is read first and oversized images are rejected.
/// </summary>
public static class SafeImageLoader
{
    // Roughly 8000x5000; larger than any phone camera, far below what would threaten the App Service plan
    public const long MaxPixels = 40_000_000;

    /// <exception cref="InvalidImageContentException">The image is larger than <see cref="MaxPixels"/>.</exception>
    /// <exception cref="UnknownImageFormatException">The data is not a supported image.</exception>
    public static async Task<Image> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var seekable = stream;
        if (!stream.CanSeek)
        {
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            seekable = buffer;
        }

        var start = seekable.Position;
        var info = await Image.IdentifyAsync(seekable, cancellationToken);
        if ((long)info.Width * info.Height > MaxPixels)
        {
            throw new InvalidImageContentException($"Image of {info.Width}x{info.Height} exceeds the {MaxPixels} pixel limit");
        }

        seekable.Position = start;
        return await Image.LoadAsync(new DecoderOptions { MaxFrames = 1 }, seekable, cancellationToken);
    }
}
