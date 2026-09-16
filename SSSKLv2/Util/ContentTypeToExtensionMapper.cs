namespace SSSKLv2.Util
{
    public static class ContentTypeToExtensionMapper
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            { "image/jpeg", ".jpg" },
            { "image/jpg", ".jpg" },
            { "image/png", ".png" },
            { "image/webp", ".webp" },
            { "image/heic", ".heic" },
            { "image/heif", ".heif" },
            { "image/heic-sequence", ".heic" },
            { "image/heif-sequence", ".heif" }
        };

        private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".webp", "image/webp" },
            { ".heic", "image/heic" },
            { ".heif", "image/heif" }
        };

        public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/heic",
            "image/heif",
            "image/heic-sequence",
            "image/heif-sequence"
        };

        public static string? GetExtension(string contentType)
        {
            if (NormalizeContentType(contentType) is not { } normalized)
                return null;

            return ContentTypeToExtension.TryGetValue(normalized, out var ext) ? ext : null;
        }

        public static string? GetContentType(string extension)
        {
            if (ExtensionToContentType.TryGetValue(extension, out var ext))
                return ext;
            return null;
        }

        public static string? NormalizeContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return null;

            var trimmed = contentType.Trim();
            if (!AllowedContentTypes.Contains(trimmed))
                return null;

            return trimmed.ToLowerInvariant() switch
            {
                "image/jpg" => "image/jpeg",
                "image/heic-sequence" => "image/heic",
                "image/heif-sequence" => "image/heif",
                _ => trimmed.ToLowerInvariant()
            };
        }

        public static string? NormalizeContentType(string? contentType, string? fileName)
        {
            var normalized = NormalizeContentType(contentType);
            if (normalized is not null)
                return normalized;

            if (string.IsNullOrWhiteSpace(fileName) ||
                (!string.IsNullOrWhiteSpace(contentType) &&
                 !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase) &&
                 !contentType.Equals("binary/octet-stream", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var extension = Path.GetExtension(fileName);
            return GetContentType(extension);
        }

        public static bool IsAllowedContentType(string? contentType)
        {
            return NormalizeContentType(contentType) is not null;
        }
    }
}

