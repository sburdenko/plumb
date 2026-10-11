using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Plumb.Viewer
{
    /// <summary>
    /// Times a viewer run for the README: how long until the model is read, shown and clickable, then the frame
    /// rate while the camera turns around it. Times are seconds from <see cref="UnityEngine.Time.realtimeSinceStartupAsDouble"/>.
    /// </summary>
    public sealed class BenchmarkRecorder
    {
        /// <summary>Frames skipped before sampling, while shaders compile and caches fill.</summary>
        public const int WarmUpFrames = 30;

        public const double SampleSeconds = 5;

        private const double Tolerance = 1e-6;

        private readonly double _start;
        private readonly List<double> _frames = new List<double>();
        private double _modelRead;
        private double _sceneShown;
        private double _collidersReady;
        private int _warmUpLeft = WarmUpFrames;
        private double _sampled;

        public BenchmarkRecorder(double start)
        {
            _start = start;
        }

        public void ModelRead(double now) => _modelRead = now - _start;

        public void SceneShown(double now) => _sceneShown = now - _start;

        public void CollidersReady(double now) => _collidersReady = now - _start;

        /// <returns>True once enough frames are sampled.</returns>
        public bool AddFrame(double deltaSeconds)
        {
            if (_warmUpLeft > 0)
            {
                _warmUpLeft--;
                return false;
            }

            _frames.Add(deltaSeconds);
            _sampled += deltaSeconds;
            return _sampled >= SampleSeconds - Tolerance;
        }

        /// <summary>"| read | shown | clickable | average fps | 1% low fps | meshes | triangles |".</summary>
        public string Report(int meshes, long triangles)
        {
            var slowestFirst = _frames.OrderByDescending(frame => frame).ToList();
            var onePercent = slowestFirst.Count == 0 ? 0 : slowestFirst[(int)Math.Ceiling(slowestFirst.Count * 0.01) - 1];
            var cells = new[]
            {
                Seconds(_modelRead),
                Seconds(_sceneShown),
                Seconds(_collidersReady),
                Fps(_sampled == 0 ? 0 : _frames.Count / _sampled),
                Fps(onePercent == 0 ? 0 : 1 / onePercent),
                meshes.ToString("N0", CultureInfo.InvariantCulture),
                Count(triangles),
            };
            return "| " + string.Join(" | ", cells) + " |";
        }

        private static string Count(long count) => count >= 1_000_000
            ? (count / 1e6).ToString("0.0", CultureInfo.InvariantCulture) + " M"
            : (count / 1e3).ToString("0", CultureInfo.InvariantCulture) + " k";

        private static string Seconds(double seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";

        private static string Fps(double fps) => Math.Round(fps).ToString("0", CultureInfo.InvariantCulture) + " fps";
    }
}
