namespace SSSKLv2.Util;

public static class Base64FileDecoder
{
    // Some clients (e.g. FileReader.readAsDataURL) include a "data:<mime>;base64," prefix.
    public static bool TryDecode(string? base64Content, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(base64Content))
            return false;

        var commaIndex = base64Content.IndexOf(',');
        var payload = base64Content.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
            ? base64Content[(commaIndex + 1)..]
            : base64Content;

        try
        {
            bytes = Convert.FromBase64String(payload);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
