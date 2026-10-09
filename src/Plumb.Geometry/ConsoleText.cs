using System.Text;

namespace Plumb.Geometry;

/// <summary>
/// Decodes a child process's console output. IfcConvert writes UTF-8 on macOS and Linux but UTF-16 on
/// Windows, so the encoding is detected from the bytes instead of assumed from the platform.
/// </summary>
internal static class ConsoleText
{
    // Mostly-ASCII UTF-16LE text has a zero in nearly every odd byte; UTF-8 text has none.
    private const double Utf16ZeroShare = 0.3;

    public static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }

        return LooksLikeUtf16(bytes) ? Encoding.Unicode.GetString(bytes) : Encoding.UTF8.GetString(bytes);
    }

    private static bool LooksLikeUtf16(byte[] bytes)
    {
        var pairs = bytes.Length / 2;
        if (pairs == 0)
        {
            return false;
        }

        var zeros = 0;
        for (var i = 1; i < bytes.Length; i += 2)
        {
            if (bytes[i] == 0)
            {
                zeros++;
            }
        }

        return zeros >= pairs * Utf16ZeroShare;
    }
}
