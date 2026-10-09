using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace Plumb.Geometry;

public static class GlbInfo
{
    private const int HeaderLength = 12;
    private const int ChunkHeaderLength = 8;
    private const uint JsonChunkType = 0x4E4F534A;

    /// <summary>Counts the nodes in a binary glTF file by reading only its JSON chunk.</summary>
    /// <returns>The node count, or null when the file is missing or not a binary glTF.</returns>
    public static int? CountNodes(string glbPath)
    {
        try
        {
            using var stream = File.OpenRead(glbPath);
            Span<byte> header = stackalloc byte[HeaderLength + ChunkHeaderLength];
            if (stream.Read(header) < header.Length || Encoding.ASCII.GetString(header[..4]) != "glTF")
            {
                return null;
            }

            var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(HeaderLength, 4));
            var chunkType = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(HeaderLength + 4, 4));
            if (chunkType != JsonChunkType || jsonLength <= 0)
            {
                return null;
            }

            var json = new byte[jsonLength];
            stream.ReadExactly(json);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("nodes", out var nodes) ? nodes.GetArrayLength() : 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or EndOfStreamException)
        {
            return null;
        }
    }
}
