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
            { "image/heif", ".heif" }
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
            "image/heif"
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
                _ => trimmed.ToLowerInvariant()
            };
        }

        public static bool IsAllowedContentType(string? contentType)
        {
            return NormalizeContentType(contentType) is not null;
        }
    }
}

