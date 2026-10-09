using System.Buffers.Binary;
using System.Text.Json;

namespace Plumb.Tests;

/// <summary>
/// Reads node names from the JSON chunk of a binary glTF file.
/// </summary>
internal static class GlbFile
{
    public static IReadOnlyList<string> NodeNames(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("glTF"), "not a binary glTF file");

        var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4));
        using var json = JsonDocument.Parse(bytes.AsMemory(20, jsonLength));
        return json.RootElement.GetProperty("nodes").EnumerateArray()
            .Where(node => node.TryGetProperty("name", out _))
            .Select(node => node.GetProperty("name").GetString()!)
            .ToList();
    }
}
