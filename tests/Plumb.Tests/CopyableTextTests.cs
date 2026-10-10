using Microsoft.Extensions.Time.Testing;
using Plumb.App.Platform;
using Plumb.App.Screens.Model.Properties;

namespace Plumb.Tests;

[TestFixture]
public sealed class CopyableTextTests
{
    private FakeTimeProvider _clock = null!;
    private FakeClipboard _clipboard = null!;

    [SetUp]
    public void CreateFakes()
    {
        _clock = new FakeTimeProvider();
        _clipboard = new FakeClipboard();
    }

    [Test]
    public void CopyingPutsTheTextOnTheClipboardAndSaysSoForAMoment()
    {
        var text = new CopyableTextViewModel("2O2Fr$t4X7Zf8NOew3FNqI", _clipboard, _clock);

        var copying = text.CopyCommand.ExecuteAsync(null);

        Assert.That(_clipboard.Text, Is.EqualTo("2O2Fr$t4X7Zf8NOew3FNqI"));
        Assert.That(text.State, Is.EqualTo(CopyState.Copied));

        _clock.Advance(CopyableTextViewModel.FeedbackDuration);

        Assert.That(copying.IsCompletedSuccessfully, Is.True);
        Assert.That(text.State, Is.EqualTo(CopyState.Idle));
    }

    [Test]
    public void AFailedCopySaysSo()
    {
        _clipboard.Works = false;
        var text = new CopyableTextViewModel("W1", _clipboard, _clock);

        _ = text.CopyCommand.ExecuteAsync(null);

        Assert.That(text.State, Is.EqualTo(CopyState.Failed));
        Assert.That(text.Feedback, Is.EqualTo("Could not copy"));
    }
}
