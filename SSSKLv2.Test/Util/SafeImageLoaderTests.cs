using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SSSKLv2.Util;

namespace SSSKLv2.Test.Util;

[TestClass]
public class SafeImageLoaderTests
{
    private static byte[] Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    // Rewrites the IHDR dimensions of a PNG without touching the pixel data: a cheap "decompression bomb"
    private static byte[] WithDeclaredSize(byte[] png, int width, int height)
    {
        var bytes = (byte[])png.Clone();
        const int ihdrData = 16; // 8 byte signature + 4 length + 4 type
        WriteBigEndian(bytes, ihdrData, width);
        WriteBigEndian(bytes, ihdrData + 4, height);
        var crc = Crc32(bytes.AsSpan(12, 4 + 13));
        WriteBigEndian(bytes, ihdrData + 13, unchecked((int)crc));
        return bytes;
    }

    private static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }

    [TestMethod]
    public async Task LoadAsync_NormalImage_Loads()
    {
        using var image = await SafeImageLoader.LoadAsync(new MemoryStream(Png(20, 10)));

        image.Width.Should().Be(20);
        image.Height.Should().Be(10);
    }

    [TestMethod]
    public async Task LoadAsync_DeclaredSizeOverLimit_Throws()
    {
        var bomb = WithDeclaredSize(Png(1, 1), 50_000, 50_000);

        var act = () => SafeImageLoader.LoadAsync(new MemoryStream(bomb));

        await act.Should().ThrowAsync<InvalidImageContentException>();
    }

    [TestMethod]
    public async Task LoadAsync_NonSeekableStream_Loads()
    {
        var stream = new NonSeekableStream(Png(4, 4));

        using var image = await SafeImageLoader.LoadAsync(stream);

        image.Width.Should().Be(4);
    }

    [TestMethod]
    public async Task LoadAsync_NotAnImage_ThrowsImageFormatException()
    {
        var act = () => SafeImageLoader.LoadAsync(new MemoryStream("hello"u8.ToArray()));

        await act.Should().ThrowAsync<ImageFormatException>();
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
