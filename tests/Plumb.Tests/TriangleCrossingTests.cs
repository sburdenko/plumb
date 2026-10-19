using System.Numerics;
using Plumb.Core.Clashes;

namespace Plumb.Tests;

[TestFixture]
public sealed class TriangleCrossingTests
{
    private static readonly Triangle Floor = new(new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(0, 1, 0));

    [Test]
    public void TrianglesThatPassThroughEachOtherCrossByTheShallowerSide()
    {
        var post = new Triangle(new Vector3(0, 0, -0.25f), new Vector3(0, 0.1f, 2), new Vector3(0, -0.1f, 2));

        Assert.That(TriangleCrossing.Depth(Floor, post), Is.EqualTo(0.25f).Within(1e-5f));
        Assert.That(TriangleCrossing.Depth(post, Floor), Is.EqualTo(0.25f).Within(1e-5f));
    }

    [Test]
    public void ATriangleStandingOnAnotherOnlyTouchesIt()
    {
        var standing = new Triangle(new Vector3(0, 0, 0), new Vector3(0, 0.1f, 2), new Vector3(0, -0.1f, 2));

        Assert.That(TriangleCrossing.Depth(Floor, standing), Is.Zero);
    }

    [Test]
    public void TrianglesInOnePlaneDoNotCross()
    {
        var beside = new Triangle(new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(2, 2, 0));

        Assert.That(TriangleCrossing.Depth(Floor, beside), Is.Zero);
    }

    [Test]
    public void TrianglesWhosePlanesCrossAwayFromThemDoNotCross()
    {
        var aside = new Triangle(new Vector3(5, 0, -1), new Vector3(5, 0.1f, 2), new Vector3(5, -0.1f, 2));

        Assert.That(TriangleCrossing.Depth(Floor, aside), Is.Zero);
    }

    [Test]
    public void ParallelTrianglesDoNotCross()
    {
        var ceiling = new Triangle(new Vector3(-1, -1, 3), new Vector3(1, -1, 3), new Vector3(0, 1, 3));

        Assert.That(TriangleCrossing.Depth(Floor, ceiling), Is.Zero);
    }

    [Test]
    public void ADegenerateTriangleCrossesNothing()
    {
        var line = new Triangle(new Vector3(0, 0, -1), new Vector3(0, 0, 0), new Vector3(0, 0, 1));

        Assert.That(TriangleCrossing.Depth(Floor, line), Is.Zero);
    }
}
