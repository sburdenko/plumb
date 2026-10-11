using NUnit.Framework;

namespace Plumb.Viewer.Tests
{
    public sealed class BenchmarkRecorderTests
    {
        [Test]
        public void LoadStagesAreTimedFromTheStart()
        {
            var recorder = new BenchmarkRecorder(10.0);
            recorder.ModelRead(12.5);
            recorder.SceneShown(13.0);
            recorder.CollidersReady(20.0);

            Assert.That(recorder.Report(1234, 2_100_000), Does.StartWith("| 2.5 s | 3.0 s | 10.0 s |"));
        }

        [Test]
        public void WarmUpFramesAreIgnoredAndSamplingStopsAfterFiveSeconds()
        {
            var recorder = new BenchmarkRecorder(0);
            for (var i = 0; i < BenchmarkRecorder.WarmUpFrames; i++)
            {
                Assert.That(recorder.AddFrame(0.5), Is.False);
            }

            var frames = 0;
            while (!recorder.AddFrame(0.01))
            {
                frames++;
            }

            Assert.That(frames + 1, Is.EqualTo(500));
            Assert.That(recorder.Report(1, 1), Does.Contain("| 100 fps |"));
        }

        [TestCase(2_100_000, "2.1 M")]
        [TestCase(41_500, "42 k")]
        public void TriangleCountsUseMillionsOrThousands(long triangles, string expected)
        {
            Assert.That(new BenchmarkRecorder(0).Report(1, triangles), Does.EndWith($"| {expected} |"));
        }

        [Test]
        public void TheOnePercentLowComesFromTheSlowestFrames()
        {
            var recorder = new BenchmarkRecorder(0);
            for (var i = 0; i < BenchmarkRecorder.WarmUpFrames; i++)
            {
                recorder.AddFrame(0.01);
            }

            for (var i = 0; i < 490; i++)
            {
                recorder.AddFrame(0.01);
            }

            for (var i = 0; i < 5; i++)
            {
                recorder.AddFrame(0.02);
            }

            Assert.That(recorder.Report(1234, 2_100_000), Does.EndWith("| 99 fps | 50 fps | 1,234 | 2.1 M |"));
        }
    }
}
