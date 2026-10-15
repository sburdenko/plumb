namespace Plumb.Viewer
{
    /// <summary><c>--screenshot</c>: once the scene is ready, or the viewer stopped with a message, save a frame and quit.</summary>
    public sealed class ScreenshotAutomation : ViewerAutomation
    {
        private readonly string _path;
        private Stage _stage = Stage.WaitingForScene;
        private int _framesLeft;

        public ScreenshotAutomation(string path)
        {
            _path = path;
        }

        private enum Stage
        {
            WaitingForScene,
            CountingDownToCapture,
            CountingDownToQuit,
            Done,
        }

        public override void Ready(IViewerHost host, double now) => StartCountdown();

        public override void Stopped(IViewerHost host, string message) => StartCountdown();

        public override void Update(IViewerHost host, double deltaSeconds)
        {
            switch (_stage)
            {
                case Stage.CountingDownToCapture when --_framesLeft == 0:
                    host.CaptureScreenshot(_path);
                    _stage = Stage.CountingDownToQuit;
                    _framesLeft = FramesBetweenSteps;
                    break;
                case Stage.CountingDownToQuit when --_framesLeft == 0:
                    host.Quit();
                    _stage = Stage.Done;
                    break;
            }
        }

        private void StartCountdown()
        {
            if (_stage == Stage.WaitingForScene)
            {
                _stage = Stage.CountingDownToCapture;
                _framesLeft = FramesBetweenSteps;
            }
        }
    }
}
