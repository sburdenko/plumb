using System.Collections.Generic;
using NUnit.Framework;

namespace Plumb.Viewer.Tests
{
    public sealed class ViewerAutomationTests
    {
        [Test]
        public void AnInteractiveRunNeverCapturesWritesOrQuits()
        {
            var host = new FakeHost();
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--package", "/m/a.plumb" }));

            automation.Begin(host, 0);
            automation.Ready(host, 1);
            Frames(automation, host, 20, 0.01);

            Assert.That(host.Calls, Is.Empty);
        }

        [Test]
        public void AScreenshotWaitsForTheSceneThenCapturesOnceAndQuits()
        {
            var host = new FakeHost();
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--screenshot", "/tmp/shot.png" }));
            automation.Begin(host, 0);

            Frames(automation, host, 10, 0.01);
            Assert.That(host.Calls, Is.Empty, "nothing happens before the scene is ready");

            automation.Ready(host, 1);
            Frames(automation, host, 20, 0.01);

            Assert.That(host.Calls, Is.EqualTo(new[] { "capture /tmp/shot.png", "quit" }));
        }

        [Test]
        public void AScreenshotOfAViewerThatStoppedShowsItsMessage()
        {
            var host = new FakeHost();
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--screenshot", "/tmp/shot.png" }));
            automation.Begin(host, 0);

            automation.Stopped(host, "Package not found");
            Frames(automation, host, 20, 0.01);

            Assert.That(host.Calls, Is.EqualTo(new[] { "capture /tmp/shot.png", "quit" }));
        }

        [Test]
        public void ABenchmarkUncapsTheFrameRateTurnsTheCameraWritesItsReportAndQuits()
        {
            var host = new FakeHost();
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--benchmark", "/tmp/bench.txt" }));

            automation.Begin(host, 10);
            automation.ModelRead(10.5);
            automation.SceneShown(11);
            automation.Ready(host, 12);
            Frames(automation, host, BenchmarkRecorder.WarmUpFrames + 600, 0.01);

            Assert.That(host.Calls[0], Is.EqualTo("uncap"));
            Assert.That(host.Calls, Has.Some.StartsWith("write /tmp/bench.txt | 0.5 s | 1.0 s | 2.0 s | 100 fps"));
            Assert.That(host.Calls[host.Calls.Count - 1], Is.EqualTo("quit"));
            Assert.That(host.TurnedDegrees, Is.GreaterThan(0f));
        }

        [Test]
        public void ABenchmarkOfAViewerThatStoppedWritesTheReason()
        {
            var host = new FakeHost();
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--benchmark", "/tmp/bench.txt" }));
            automation.Begin(host, 0);

            automation.Stopped(host, "Package not found");
            Frames(automation, host, 20, 0.01);

            Assert.That(host.Calls, Is.EqualTo(new[] { "uncap", "write /tmp/bench.txt | failed: Package not found |", "quit" }));
        }

        [Test]
        public void ABenchmarkTakesPrecedenceOverAScreenshot()
        {
            var automation = ViewerAutomation.For(ViewerArguments.Parse(new[] { "viewer", "--screenshot", "/tmp/s.png", "--benchmark", "/tmp/b.txt" }));

            Assert.That(automation, Is.TypeOf<BenchmarkAutomation>());
        }

        private static void Frames(ViewerAutomation automation, FakeHost host, int count, double deltaSeconds)
        {
            for (var i = 0; i < count; i++)
            {
                automation.Update(host, deltaSeconds);
            }
        }

        private sealed class FakeHost : IViewerHost
        {
            public List<string> Calls { get; } = new List<string>();

            public float TurnedDegrees { get; private set; }

            public void CaptureScreenshot(string path) => Calls.Add($"capture {path}");

            public void WriteReport(string path, string text) => Calls.Add($"write {path} {text}");

            public void TurnCamera(float degrees) => TurnedDegrees += degrees;

            public (int Meshes, long Triangles) CountGeometry() => (1, 1);

            public void UncapFrameRate() => Calls.Add("uncap");

            public void Quit() => Calls.Add("quit");
        }
    }
}
