using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Plumb.Tests;

/// <summary>Writes small binary glTF files for tests: one triangle mesh per <see cref="AddMesh"/>, nodes as JSON.</summary>
internal sealed class GlbBuilder
{
    private readonly MemoryStream _binary = new();
    private readonly JsonArray _meshes = [];
    private readonly JsonArray _accessors = [];
    private readonly JsonArray _bufferViews = [];

    public JsonArray Nodes { get; } = [];

    public JsonArray SceneNodes { get; } = [];

    /// <returns>The mesh index.</returns>
    public int AddMesh(float[] positions, int[]? indices = null, bool wideIndices = false, int mode = 4, int positionStride = 0)
    {
        var attributes = new JsonObject { ["POSITION"] = AddPositions(positions, positionStride) };
        var primitive = new JsonObject { ["attributes"] = attributes, ["mode"] = mode };
        if (indices != null)
        {
            primitive["indices"] = AddIndices(indices, wideIndices);
        }

        _meshes.Add(new JsonObject { ["primitives"] = new JsonArray(primitive) });
        return _meshes.Count - 1;
    }

    /// <returns>The node index.</returns>
    public int AddNode(JsonObject node, bool inScene = true)
    {
        Nodes.Add(node);
        if (inScene)
        {
            SceneNodes.Add(Nodes.Count - 1);
        }

        return Nodes.Count - 1;
    }

    public string Write(string path)
    {
        var binary = _binary.ToArray();
        var gltf = new JsonObject
        {
            ["asset"] = new JsonObject { ["version"] = "2.0" },
            ["scene"] = 0,
            ["scenes"] = new JsonArray(new JsonObject { ["nodes"] = SceneNodes.DeepClone() }),
            ["nodes"] = Nodes.DeepClone(),
            ["meshes"] = _meshes.DeepClone(),
            ["accessors"] = _accessors.DeepClone(),
            ["bufferViews"] = _bufferViews.DeepClone(),
            ["buffers"] = new JsonArray(new JsonObject { ["byteLength"] = binary.Length }),
        };
        var json = Pad(Encoding.UTF8.GetBytes(gltf.ToJsonString()), (byte)' ');
        var bin = Pad(binary, 0);

        using var file = File.Create(path);
        Write(file, 0x46546C67u, 2u, (uint)(12 + 8 + json.Length + 8 + bin.Length));
        Write(file, (uint)json.Length, 0x4E4F534Au);
        file.Write(json);
        Write(file, (uint)bin.Length, 0x004E4942u);
        file.Write(bin);
        return path;
    }

    private int AddPositions(float[] positions, int stride)
    {
        var offset = Align();
        var elementSize = stride == 0 ? 12 : stride;
        var count = positions.Length / 3;
        for (var i = 0; i < count; i++)
        {
            var element = new byte[elementSize];
            for (var axis = 0; axis < 3; axis++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(element.AsSpan(axis * 4), positions[i * 3 + axis]);
            }

            _binary.Write(element);
        }

        var view = new JsonObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = count * elementSize };
        if (stride != 0)
        {
            view["byteStride"] = stride;
        }

        return AddAccessor(view, componentType: 5126, count, "VEC3");
    }

    private int AddIndices(int[] indices, bool wide)
    {
        var offset = Align();
        foreach (var index in indices)
        {
            var bytes = new byte[wide ? 4 : 2];
            if (wide)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)index);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)index);
            }

            _binary.Write(bytes);
        }

        var view = new JsonObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = indices.Length * (wide ? 4 : 2) };
        return AddAccessor(view, wide ? 5125 : 5123, indices.Length, "SCALAR");
    }

    private int AddAccessor(JsonObject view, int componentType, int count, string type)
    {
        _bufferViews.Add(view);
        _accessors.Add(new JsonObject
        {
            ["bufferView"] = _bufferViews.Count - 1,
            ["componentType"] = componentType,
            ["count"] = count,
            ["type"] = type,
        });
        return _accessors.Count - 1;
    }

    private int Align()
    {
        while (_binary.Length % 4 != 0)
        {
            _binary.WriteByte(0);
        }

        return (int)_binary.Length;
    }

    private static byte[] Pad(byte[] bytes, byte filler) =>
        bytes.Length % 4 == 0 ? bytes : [.. bytes, .. Enumerable.Repeat(filler, 4 - bytes.Length % 4)];

    private static void Write(Stream stream, params uint[] values)
    {
        Span<byte> bytes = stackalloc byte[4];
        foreach (var value in values)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            stream.Write(bytes);
        }
    }
}
