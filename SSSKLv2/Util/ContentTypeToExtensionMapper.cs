namespace SSSKLv2.Util
{
    public static class ContentTypeToExtensionMapper
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension = new()
        {
            { "image/jpeg", ".jpeg" },
            { "image/jpg", ".jpg" },
            { "image/png", ".png" }
        };
        
        private static readonly Dictionary<string, string> ExtensionToContentType = new()
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" }
        };

        public static string? GetExtension(string contentType)
        {
            if (ContentTypeToExtension.TryGetValue(contentType, out var ext))
                return ext;
            return null;
        }
        
        public static string? GetContentType(string extension)
        {
            if (ExtensionToContentType.TryGetValue(extension, out var ext))
                return ext;
            return null;
        }
    }
}

