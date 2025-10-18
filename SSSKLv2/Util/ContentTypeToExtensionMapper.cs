namespace SSSKLv2.Util
{
    public static class ContentTypeToExtensionMapper
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension = new()
        {
            { "image/jpeg", ".jpg" },
            { "image/jpeg", ".jpeg" },
            { "image/png", ".png" }
            // Add more as needed
        };

        public static string? GetExtension(string contentType)
        {
            if (ContentTypeToExtension.TryGetValue(contentType, out var ext))
                return ext;
            return null;
        }
    }
}

