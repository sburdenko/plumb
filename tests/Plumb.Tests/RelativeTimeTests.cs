using Plumb.App.Screens.Start.Recent;

namespace Plumb.Tests;

[TestFixture]
public sealed class RelativeTimeTests
{
    private static readonly TimeSpan Kyiv = TimeSpan.FromHours(3);
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 30, 0, Kyiv);

    [TestCase(0, "just now")]
    [TestCase(-0.5, "just now")]
    [TestCase(1, "1 min ago")]
    [TestCase(59, "59 min ago")]
    [TestCase(60, "1 h ago")]
    [TestCase(15 * 60 + 29, "15 h ago")]
    [TestCase(15 * 60 + 31, "yesterday")]
    [TestCase(2 * 24 * 60, "2 days ago")]
    [TestCase(6 * 24 * 60, "6 days ago")]
    [TestCase(7 * 24 * 60, "2 Oct")]
    public void DescribesHowLongAgo(double minutesAgo, string expected)
    {
        Assert.That(RelativeTime.Format(Now.AddMinutes(-minutesAgo), Now), Is.EqualTo(expected));
    }

    [Test]
    public void ShowsTheYearForAnEarlierYear()
    {
        Assert.That(RelativeTime.Format(new DateTimeOffset(2025, 12, 28, 10, 0, 0, Kyiv), Now), Is.EqualTo("28 Dec 2025"));
    }

    [Test]
    public void CountsDaysInTheViewersTimeZone()
    {
        var lateLastNightInUtc = new DateTimeOffset(2026, 10, 8, 22, 0, 0, TimeSpan.Zero);

        Assert.That(RelativeTime.Format(lateLastNightInUtc, Now), Is.EqualTo("14 h ago"));
    }
}
