namespace Plumb.Viewer
{
    /// <summary>
    /// <c>--benchmark</c>: times loading, then turns the camera around the model while frames are sampled, writes the
    /// report and quits. A viewer that stopped with a message writes the message instead.
    /// </summary>
    public sealed class BenchmarkAutomation : ViewerAutomation
    {
        private const float DegreesPerSecond = 30f;

        private readonly string _path;
        private BenchmarkRecorder _recorder = new BenchmarkRecorder(0);
        private Stage _stage = Stage.WaitingForScene;
        private int _framesLeft;

        public BenchmarkAutomation(string path)
        {
            _path = path;
        }

        private enum Stage
        {
            WaitingForScene,
            Sampling,
            CountingDownToQuit,
            Done,
        }

        public override void Begin(IViewerHost host, double now)
        {
            host.UncapFrameRate();
            _recorder = new BenchmarkRecorder(now);
        }

        public override void ModelRead(double now) => _recorder.ModelRead(now);

        public override void SceneShown(double now) => _recorder.SceneShown(now);

        public override void Ready(IViewerHost host, double now)
        {
            if (_stage == Stage.WaitingForScene)
            {
                _recorder.CollidersReady(now);
                _stage = Stage.Sampling;
            }
        }

        public override void Stopped(IViewerHost host, string message)
        {
            if (_stage == Stage.WaitingForScene)
            {
                Finish(host, $"| failed: {message} |");
            }
        }

        public override void Update(IViewerHost host, double deltaSeconds)
        {
            switch (_stage)
            {
                case Stage.Sampling:
                    host.TurnCamera((float)(DegreesPerSecond * deltaSeconds));
                    if (_recorder.AddFrame(deltaSeconds))
                    {
                        var (meshes, triangles) = host.CountGeometry();
                        Finish(host, _recorder.Report(meshes, triangles));
                    }

                    break;
                case Stage.CountingDownToQuit when --_framesLeft == 0:
                    host.Quit();
                    _stage = Stage.Done;
                    break;
            }
        }

        private void Finish(IViewerHost host, string report)
        {
            host.WriteReport(_path, report);
            _stage = Stage.CountingDownToQuit;
            _framesLeft = FramesBetweenSteps;
        }
    }
}
