using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Util;

namespace SSSKLv2.Test.Util;

[TestClass]
public class ContentTypeToExtensionMapperTests
{
    [TestMethod]
    [DataRow("image/jpeg", ".jpg")]
    [DataRow("image/jpg", ".jpg")]
    [DataRow("image/png", ".png")]
    [DataRow("image/heic-sequence", ".heic")]
    public void GetExtension_KnownContentType_ReturnsExtension(string contentType, string expected)
    {
        var result = ContentTypeToExtensionMapper.GetExtension(contentType);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void GetExtension_UnknownContentType_ReturnsNull()
    {
        var result = ContentTypeToExtensionMapper.GetExtension("application/pdf");
        result.Should().BeNull();
    }

    [TestMethod]
    [DataRow(".jpg", "image/jpeg")]
    [DataRow(".jpeg", "image/jpeg")]
    [DataRow(".png", "image/png")]
    public void GetContentType_KnownExtension_ReturnsContentType(string extension, string expected)
    {
        var result = ContentTypeToExtensionMapper.GetContentType(extension);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void GetContentType_UnknownExtension_ReturnsNull()
    {
        var result = ContentTypeToExtensionMapper.GetContentType(".pdf");
        result.Should().BeNull();
    }

    [TestMethod]
    [DataRow("application/octet-stream", "photo.HEIC", "image/heic")]
    [DataRow("", "photo.heif", "image/heif")]
    public void NormalizeContentType_GenericOrMissingMime_UsesKnownImageExtension(string contentType, string fileName, string expected)
    {
        var result = ContentTypeToExtensionMapper.NormalizeContentType(contentType, fileName);

        result.Should().Be(expected);
    }

    [TestMethod]
    public void NormalizeContentType_UnsupportedMime_DoesNotTrustImageExtension()
    {
        var result = ContentTypeToExtensionMapper.NormalizeContentType("application/pdf", "photo.heic");

        result.Should().BeNull();
    }
}
