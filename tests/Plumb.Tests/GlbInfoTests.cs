using System.Text;
using Plumb.Package;

namespace Plumb.Tests;

[TestFixture]
public sealed class GlbInfoTests
{
    [Test]
    public void CountsNodesFromTheJsonChunk()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "model.glb");
        File.WriteAllBytes(path, Glb("""{"asset":{"version":"2.0"},"nodes":[{"name":"A"},{"name":"B"},{"name":"C"}]}"""));

        Assert.That(GlbInfo.CountNodes(path), Is.EqualTo(3));
    }

    [Test]
    public void MissingOrForeignFilesHaveNoCount()
    {
        using var temp = new TempDirectory();
        var notGlb = Path.Combine(temp.Path, "model.glb");
        File.WriteAllText(notGlb, "glTF");

        Assert.That(GlbInfo.CountNodes(Path.Combine(temp.Path, "missing.glb")), Is.Null);
        Assert.That(GlbInfo.CountNodes(notGlb), Is.Null);
    }

    private static byte[] Glb(string json)
    {
        var padded = json.PadRight((json.Length + 3) / 4 * 4);
        var chunk = Encoding.UTF8.GetBytes(padded);
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("glTF"));
        bytes.AddRange(BitConverter.GetBytes(2u));
        bytes.AddRange(BitConverter.GetBytes((uint)(12 + 8 + chunk.Length)));
        bytes.AddRange(BitConverter.GetBytes((uint)chunk.Length));
        bytes.AddRange(BitConverter.GetBytes(0x4E4F534Au));
        bytes.AddRange(chunk);
        return bytes.ToArray();
    }

}
