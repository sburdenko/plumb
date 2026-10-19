using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Plumb.Core.Clashes;

namespace Plumb.Package;

/// <summary>
/// Reads the surface of every element from a package's <c>model.glb</c>. IfcConvert names each element's node by
/// its GlobalId; a mesh on an unnamed node belongs to the nearest named node above it. Triangles are placed with
/// every transform on the way down from the scene, so they share one coordinate system.
/// </summary>
public static class GlbShapes
{
    private const uint Magic = 0x46546C67;
    private const uint JsonChunk = 0x4E4F534A;
    private const uint BinaryChunk = 0x004E4942;
    private const int Triangles = 4;

    /// <exception cref="PackageFormatException">The file is not a binary glTF this reader understands.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static IReadOnlyList<ElementShape> Read(string glbPath)
    {
        var bytes = File.ReadAllBytes(glbPath);
        var (json, binary) = Chunks(bytes);
        try
        {
            using var document = JsonDocument.Parse(json);
            return new Reader(document.RootElement, binary).Shapes();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            throw new PackageFormatException($"{Path.GetFileName(glbPath)} is not geometry Plumb can read: {ex.Message}", ex);
        }
    }

    private static (ReadOnlyMemory<byte> Json, ReadOnlyMemory<byte> Binary) Chunks(byte[] bytes)
    {
        if (bytes.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic)
        {
            throw new PackageFormatException("model.glb is not a binary glTF file.");
        }

        var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16)) != JsonChunk || jsonLength <= 0 || 20 + jsonLength > bytes.Length)
        {
            throw new PackageFormatException("model.glb has no readable JSON chunk.");
        }

        var binaryStart = 20 + jsonLength;
        var binary = binaryStart + 8 <= bytes.Length && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(binaryStart + 4)) == BinaryChunk
            ? bytes.AsMemory(binaryStart + 8, Math.Min(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(binaryStart)), bytes.Length - binaryStart - 8))
            : ReadOnlyMemory<byte>.Empty;
        return (bytes.AsMemory(20, jsonLength), binary);
    }

    private sealed class Reader(JsonElement gltf, ReadOnlyMemory<byte> binary)
    {
        private readonly Dictionary<string, List<Triangle>> _byElement = new(StringComparer.Ordinal);
        private readonly Dictionary<int, IReadOnlyList<Triangle>> _meshTriangles = [];

        // Indexing a JsonElement array walks it from the start, which is quadratic over thousands of nodes.
        private readonly JsonElement[] _nodes = ArrayOf(gltf, "nodes");
        private readonly JsonElement[] _meshes = ArrayOf(gltf, "meshes");
        private readonly JsonElement[] _accessors = ArrayOf(gltf, "accessors");
        private readonly JsonElement[] _bufferViews = ArrayOf(gltf, "bufferViews");

        public IReadOnlyList<ElementShape> Shapes()
        {
            var scene = gltf.TryGetProperty("scene", out var chosen) ? chosen.GetInt32() : 0;
            if (gltf.TryGetProperty("scenes", out var scenes) && scenes.GetArrayLength() > scene)
            {
                foreach (var node in Indices(scenes[scene], "nodes"))
                {
                    Visit(node, Matrix4x4.Identity, owner: null);
                }
            }

            return _byElement.Select(pair => new ElementShape(pair.Key, pair.Value)).ToList();
        }

        private void Visit(int index, Matrix4x4 parent, string? owner)
        {
            var node = _nodes[index];
            var world = LocalTransform(node) * parent;
            owner = node.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } named ? named : owner;

            if (owner != null && node.TryGetProperty("mesh", out var mesh) && MeshTriangles(mesh.GetInt32()) is { Count: > 0 } local)
            {
                if (!_byElement.TryGetValue(owner, out var triangles))
                {
                    triangles = [];
                    _byElement.Add(owner, triangles);
                }

                triangles.AddRange(local.Select(t =>
                    new Triangle(Vector3.Transform(t.A, world), Vector3.Transform(t.B, world), Vector3.Transform(t.C, world))));
            }

            foreach (var child in Indices(node, "children"))
            {
                Visit(child, world, owner);
            }
        }

        private IReadOnlyList<Triangle> MeshTriangles(int mesh)
        {
            if (_meshTriangles.TryGetValue(mesh, out var cached))
            {
                return cached;
            }

            var triangles = new List<Triangle>();
            foreach (var primitive in _meshes[mesh].GetProperty("primitives").EnumerateArray())
            {
                var mode = primitive.TryGetProperty("mode", out var declared) ? declared.GetInt32() : Triangles;
                if (mode != Triangles || !primitive.GetProperty("attributes").TryGetProperty("POSITION", out var position))
                {
                    continue;
                }

                var corners = Positions(position.GetInt32());
                var indices = primitive.TryGetProperty("indices", out var indexAccessor)
                    ? Indices(indexAccessor.GetInt32())
                    : Enumerable.Range(0, corners.Count).ToList();
                for (var i = 0; i + 2 < indices.Count; i += 3)
                {
                    triangles.Add(new Triangle(corners[indices[i]], corners[indices[i + 1]], corners[indices[i + 2]]));
                }
            }

            _meshTriangles.Add(mesh, triangles);
            return triangles;
        }

        private List<Vector3> Positions(int accessor)
        {
            var (data, count, stride) = View(accessor, elementSize: 12);
            var positions = new List<Vector3>(count);
            for (var i = 0; i < count; i++)
            {
                var at = data.Span.Slice(i * stride, 12);
                positions.Add(new Vector3(
                    BinaryPrimitives.ReadSingleLittleEndian(at),
                    BinaryPrimitives.ReadSingleLittleEndian(at[4..]),
                    BinaryPrimitives.ReadSingleLittleEndian(at[8..])));
            }

            return positions;
        }

        private List<int> Indices(int accessor)
        {
            var componentType = _accessors[accessor].GetProperty("componentType").GetInt32();
            var size = componentType switch
            {
                5121 => 1,
                5123 => 2,
                5125 => 4,
                _ => throw new InvalidOperationException($"index component type {componentType} is not supported"),
            };
            var (data, count, stride) = View(accessor, size);
            var indices = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var at = data.Span[(i * stride)..];
                indices.Add(size switch
                {
                    1 => at[0],
                    2 => BinaryPrimitives.ReadUInt16LittleEndian(at),
                    _ => checked((int)BinaryPrimitives.ReadUInt32LittleEndian(at)),
                });
            }

            return indices;
        }

        private (ReadOnlyMemory<byte> Data, int Count, int Stride) View(int accessorIndex, int elementSize)
        {
            var accessor = _accessors[accessorIndex];
            var count = accessor.GetProperty("count").GetInt32();
            var view = _bufferViews[accessor.GetProperty("bufferView").GetInt32()];
            var stride = view.TryGetProperty("byteStride", out var declared) ? declared.GetInt32() : elementSize;
            var start = Offset(view) + Offset(accessor);
            var length = count == 0 ? 0 : (count - 1) * stride + elementSize;
            return (binary.Slice(start, length), count, stride);
        }

        private static JsonElement[] ArrayOf(JsonElement gltf, string property) =>
            gltf.TryGetProperty(property, out var array) ? array.EnumerateArray().ToArray() : [];

        private static int Offset(JsonElement element) => element.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0;

        private static IEnumerable<int> Indices(JsonElement element, string property) =>
            element.TryGetProperty(property, out var list) ? list.EnumerateArray().Select(item => item.GetInt32()) : [];

        private static Matrix4x4 LocalTransform(JsonElement node)
        {
            if (node.TryGetProperty("matrix", out var matrix))
            {
                // glTF stores the matrix column by column, which is the row layout System.Numerics expects for its
                // row-vector convention.
                var m = matrix.EnumerateArray().Select(value => value.GetSingle()).ToArray();
                return new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
            }

            var scale = Vector(node, "scale", Vector3.One);
            var translation = Vector(node, "translation", Vector3.Zero);
            var rotation = node.TryGetProperty("rotation", out var r) && r.GetArrayLength() == 4
                ? new Quaternion(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle())
                : Quaternion.Identity;
            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        }

        private static Vector3 Vector(JsonElement node, string property, Vector3 fallback) =>
            node.TryGetProperty(property, out var v) && v.GetArrayLength() == 3
                ? new Vector3(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle())
                : fallback;
    }
}
