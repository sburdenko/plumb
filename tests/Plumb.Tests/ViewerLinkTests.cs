using Microsoft.Extensions.Logging.Abstractions;
using Plumb.App.Platform.Viewer;

namespace Plumb.Tests;

[TestFixture]
public sealed class ViewerLinkTests
{
    private const string Duplex = "/models/Duplex.plumb";
    private const string Wall = "2O2Fr$t4X7Zf8NOew3FNql";
    private const string Door = "0K7w7JxHr9IggqQcRYZ6Jx";
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private FakeViewerStarter _starter = null!;
    private ViewerLink _link = null!;
    private readonly List<FakeViewer> _viewers = [];

    [SetUp]
    public void CreateLink()
    {
        _starter = new FakeViewerStarter();
        _link = new ViewerLink(_starter, NullLogger<ViewerLink>.Instance);
    }

    [TearDown]
    public void CloseEverything()
    {
        _viewers.ForEach(viewer => viewer.Dispose());
        _viewers.Clear();
        _link.Dispose();
    }

    [Test]
    public void OpeningStartsTheViewerWithAnInvitation()
    {
        Assert.That(_link.TryOpen(Duplex, selectedId: null, out var error), Is.True, error);

        Assert.That(_starter.Started.Single().PackagePath, Is.EqualTo(Duplex));
        Assert.That(_starter.Invitation.Port, Is.Positive);
        Assert.That(_starter.Invitation.Token, Has.Length.GreaterThanOrEqualTo(32));
    }

    [Test]
    public void EveryViewerGetsItsOwnToken()
    {
        _link.TryOpen(Duplex, null, out _);
        var first = _starter.Invitation.Token;
        _link.TryOpen("/models/Clinic.plumb", null, out _);

        Assert.That(_starter.Invitation.Token, Is.Not.EqualTo(first));
    }

    [Test]
    public void AFailedStartReportsTheReason()
    {
        _starter.Failure = "The 3D viewer was not found.";

        Assert.That(_link.TryOpen(Duplex, null, out var error), Is.False);
        Assert.That(error, Is.EqualTo("The 3D viewer was not found."));
    }

    [Test]
    public async Task ALinkedViewerReceivesTheSelectionPlumbHadWhenItOpened()
    {
        _link.TryOpen(Duplex, Wall, out _);

        var viewer = await LinkAsync();

        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Wall}"));
    }

    [Test]
    public async Task ASelectionMadeWhileTheViewerStartsIsSentOnceItLinks()
    {
        _link.TryOpen(Duplex, null, out _);
        _link.ShowSelection(Duplex, Door);

        var viewer = await LinkAsync();

        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Door}"));
    }

    [Test]
    public async Task SelectionsMadeInPlumbReachTheViewer()
    {
        var viewer = await OpenAndLinkAsync();

        _link.ShowSelection(Duplex, Wall);
        _link.ShowSelection(Duplex, null);

        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Wall}"));
        Assert.That(await viewer.ReadAsync(), Is.EqualTo("clear"));
    }

    [Test]
    public async Task ASelectionMadeInTheViewerIsReportedWithItsPackage()
    {
        var viewer = await OpenAndLinkAsync();
        var reported = NextReport();

        await viewer.SendAsync($"select {Wall}");

        Assert.That(await reported, Is.EqualTo(new ViewerSelection(Duplex, Wall)));
    }

    [Test]
    public async Task ClearingTheSelectionInTheViewerIsReported()
    {
        var viewer = await OpenAndLinkAsync();
        var selected = NextReport();
        await viewer.SendAsync($"select {Wall}");
        await selected;
        var cleared = NextReport();

        await viewer.SendAsync("clear");

        Assert.That(await cleared, Is.EqualTo(new ViewerSelection(Duplex, null)));
    }

    [Test]
    public async Task WhatTheViewerReportedIsNotSentBackToIt()
    {
        var viewer = await OpenAndLinkAsync();
        var reported = NextReport();
        await viewer.SendAsync($"select {Wall}");
        await reported;

        _link.ShowSelection(Duplex, Wall);
        _link.ShowSelection(Duplex, Door);

        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Door}"));
    }

    [Test]
    public async Task ASelectionInAnotherModelIsNotSent()
    {
        var viewer = await OpenAndLinkAsync();

        _link.ShowSelection("/models/Clinic.plumb", Wall);
        _link.ShowSelection(Duplex, Door);

        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Door}"));
    }

    [Test]
    public async Task UnknownLinesAreIgnored()
    {
        var viewer = await OpenAndLinkAsync();
        var reported = NextReport();

        await viewer.SendAsync("frame everything");
        await viewer.SendAsync($"select {Wall}");

        Assert.That(await reported, Is.EqualTo(new ViewerSelection(Duplex, Wall)));
    }

    [Test]
    public async Task AWrongTokenIsTurnedAway()
    {
        _link.TryOpen(Duplex, Wall, out _);
        var stranger = await FakeViewer.ConnectAsync(_starter.Invitation);
        _viewers.Add(stranger);

        await stranger.SendAsync("hello 1 not-the-token");

        Assert.That(await stranger.ReadAsync(), Is.Null);
    }

    [Test]
    public async Task AnotherProtocolVersionIsTurnedAway()
    {
        _link.TryOpen(Duplex, Wall, out _);
        var newer = await FakeViewer.ConnectAsync(_starter.Invitation);
        _viewers.Add(newer);

        await newer.SendAsync($"hello 2 {_starter.Invitation.Token}");

        Assert.That(await newer.ReadAsync(), Is.Null);
    }

    [Test]
    public async Task OpeningTheSameModelAgainBringsTheRunningViewerForward()
    {
        var viewer = await OpenAndLinkAsync();

        Assert.That(_link.TryOpen(Duplex, Door, out _), Is.True);

        Assert.That(_starter.Started, Has.Count.EqualTo(1));
        Assert.That(_starter.Processes.Single().TimesBroughtToFront, Is.EqualTo(1));
        Assert.That(await viewer.ReadAsync(), Is.EqualTo($"select {Door}"));
    }

    [Test]
    public void OpeningAgainWhileTheViewerIsStillStartingDoesNotStartASecondOne()
    {
        _link.TryOpen(Duplex, null, out _);
        _link.TryOpen(Duplex, null, out _);

        Assert.That(_starter.Started, Has.Count.EqualTo(1));
    }

    [Test]
    public void OpeningAfterTheViewerWasClosedStartsANewOne()
    {
        _link.TryOpen(Duplex, null, out _);
        _starter.Processes[0].HasExited = true;

        _link.TryOpen(Duplex, null, out _);

        Assert.That(_starter.Started, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task OpeningAnotherModelLetsGoOfTheFirstViewer()
    {
        var first = await OpenAndLinkAsync();

        _link.TryOpen("/models/Clinic.plumb", null, out _);

        Assert.That(_starter.Started, Has.Count.EqualTo(2));
        Assert.That(await first.ReadAsync(), Is.Null);
    }

    [Test]
    public async Task AfterTheViewerDisconnectsOpeningStartsANewOne()
    {
        var viewer = await OpenAndLinkAsync();
        viewer.Dispose();
        await WaitUntilAsync(() => _link.Session is ViewerSession.None);
        _link.TryOpen(Duplex, null, out _);

        Assert.That(_starter.Started, Has.Count.EqualTo(2));
    }

    private async Task<FakeViewer> OpenAndLinkAsync()
    {
        Assert.That(_link.TryOpen(Duplex, selectedId: null, out var error), Is.True, error);
        return await LinkAsync();
    }

    /// <summary>Links a viewer and waits until Plumb has accepted it, by a selection round trip.</summary>
    private async Task<FakeViewer> LinkAsync()
    {
        var viewer = await FakeViewer.LinkAsync(_starter.Invitation);
        _viewers.Add(viewer);
        await WaitUntilAsync(() => _link.Session is ViewerSession.Linked);
        return viewer;
    }

    private Task<ViewerSelection> NextReport()
    {
        var next = new TaskCompletionSource<ViewerSelection>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<ViewerSelection>? handler = null;
        handler = (_, selection) =>
        {
            _link.SelectionChanged -= handler;
            next.TrySetResult(selection);
        };
        _link.SelectionChanged += handler;
        return next.Task.WaitAsync(Patience);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Patience;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("Timed out waiting for the link.");
            }

            await Task.Delay(10);
        }
    }
}
