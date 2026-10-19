using Plumb.Core.Clashes;

namespace Plumb.Tests;

[TestFixture]
public sealed class ClashDetectorTests
{
    private const float Centimetre = 0.01f;

    private static readonly ElementShape Beam = Cuboid.Shape("beam", (0, 0, 0), (4, 0.3f, 0.5f));
    private static readonly ElementShape Slab = Cuboid.Shape("slab", (0, 0, 0), (4, 4, 0.2f));

    [Test]
    public void ADuctThroughABeamClashes()
    {
        var duct = Cuboid.Shape("duct", (1.8f, -1, 0.1f), (2.2f, 1.3f, 0.4f));

        var clash = Find([duct], [Beam]).Single();

        Assert.That((clash.FirstId, clash.SecondId), Is.EqualTo(("duct", "beam")));
        Assert.That(clash.Penetration, Is.GreaterThan(Centimetre));
    }

    [Test]
    public void APipeThroughADuctPenetratesByThePipesSizeNotItsLength()
    {
        var duct = Cuboid.Shape("duct", (0, 0, 0), (4, 0.6f, 0.4f));
        var pipe = Cuboid.Shape("pipe", (2, -1, 0.1f), (2.05f, 2, 0.15f));

        Assert.That(Find([pipe], [duct]).Single().Penetration, Is.EqualTo(0.05f).Within(1e-4f));
    }

    [Test]
    public void AWallStandingOnASlabOnlyTouchesIt()
    {
        var wall = Cuboid.Shape("wall", (0, 0, 0.2f), (4, 0.2f, 3));

        Assert.That(Find([wall], [Slab]), Is.Empty);
    }

    [Test]
    public void AnOverlapWithinTheToleranceIsIgnored()
    {
        var sunkFiveMillimetres = Cuboid.Shape("wall", (0, 0, 0.195f), (4, 0.2f, 3));

        Assert.That(Find([sunkFiveMillimetres], [Slab], tolerance: Centimetre), Is.Empty);
        Assert.That(Find([sunkFiveMillimetres], [Slab], tolerance: 0.001f).Single().Penetration, Is.EqualTo(0.005f).Within(1e-4f));
    }

    [Test]
    public void ElementsApartDoNotClash()
    {
        var pipe = Cuboid.Shape("pipe", (10, 10, 10), (11, 10.1f, 10.1f));

        Assert.That(Find([pipe], [Beam, Slab]), Is.Empty);
    }

    [Test]
    public void AnElementInBothGroupsIsNotComparedWithItselfAndEachPairIsReportedOnce()
    {
        var duct = Cuboid.Shape("duct", (1.8f, -1, 0.1f), (2.2f, 1.3f, 0.4f));

        var clashes = Find([duct, Beam], [Beam, duct]);

        Assert.That(clashes, Has.Count.EqualTo(1));
    }

    [Test]
    public void TheDeepestClashComesFirst()
    {
        var shallow = Cuboid.Shape("shallow", (1, 0.25f, 0.45f), (1.5f, 1, 1));
        var deep = Cuboid.Shape("deep", (3, -1, -1), (3.2f, 1, 1));

        var clashes = Find([shallow, deep], [Beam]);

        Assert.That(clashes.Select(c => c.FirstId), Is.EqualTo(new[] { "deep", "shallow" }));
    }

    [Test]
    public void TheLocationIsWhereTheElementsMeet()
    {
        var duct = Cuboid.Shape("duct", (1.8f, -1, 0.1f), (2.2f, 1.3f, 0.4f));

        var location = Find([duct], [Beam]).Single().Location;

        Assert.That(location.X, Is.InRange(1.8f, 2.2f));
        Assert.That(location.Y, Is.InRange(0, 0.3f));
        Assert.That(location.Z, Is.InRange(0.1f, 0.4f));
    }

    [Test]
    public void ASearchCanBeCancelled()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.That(
            () => ClashDetector.Find([Beam], [Slab], Centimetre, cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private static IReadOnlyList<Clash> Find(IReadOnlyList<ElementShape> first, IReadOnlyList<ElementShape> second, float tolerance = Centimetre) =>
        ClashDetector.Find(first, second, tolerance, CancellationToken.None);
}
