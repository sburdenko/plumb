using System.Numerics;
using Plumb.Core.Clashes;

namespace Plumb.Tests;

[TestFixture]
public sealed class BoxTreeTests
{
    [Test]
    public void FindsExactlyTheBoxesABruteForceSearchFinds()
    {
        var random = new Random(42);
        var boxes = Enumerable.Range(0, 2_000).Select(_ => RandomBox(random, size: 3)).ToList();
        var tree = BoxTree.Build(boxes);

        for (var query = 0; query < 200; query++)
        {
            var probe = RandomBox(random, size: 10);
            var found = new List<int>();

            tree.Query(probe, found);

            var expected = Enumerable.Range(0, boxes.Count).Where(i => boxes[i].Overlaps(probe));
            Assert.That(found, Is.EquivalentTo(expected));
        }
    }

    [Test]
    public void AnEmptyTreeFindsNothing()
    {
        var found = new List<int>();

        BoxTree.Build([]).Query(new Box(Vector3.Zero, Vector3.One), found);

        Assert.That(found, Is.Empty);
    }

    [Test]
    public void BoxesThatOnlyTouchOverlap()
    {
        var a = new Box(Vector3.Zero, Vector3.One);
        var b = new Box(new Vector3(1, 0, 0), new Vector3(2, 1, 1));
        var apart = new Box(new Vector3(1.01f, 0, 0), new Vector3(2, 1, 1));

        Assert.That(a.Overlaps(b), Is.True);
        Assert.That(a.Overlaps(apart), Is.False);
    }

    private static Box RandomBox(Random random, float size)
    {
        var min = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 30);
        return new Box(min, min + new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) * size);
    }
}
