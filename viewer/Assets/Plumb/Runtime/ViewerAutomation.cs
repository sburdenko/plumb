namespace Plumb.Viewer
{
    /// <summary>
    /// What the viewer was started to do besides being looked at. The controller reports how loading goes and calls
    /// <see cref="Update"/> every frame; each kind of run is a small state machine of its own.
    /// </summary>
    public abstract class ViewerAutomation
    {
        /// <summary>Frames to wait between one step and the next, so a frame is fully drawn before it is used.</summary>
        protected const int FramesBetweenSteps = 5;

        /// <summary><c>--benchmark</c> wins over <c>--screenshot</c>: a run does one job.</summary>
        public static ViewerAutomation For(ViewerArguments arguments)
        {
            if (arguments.RunsBenchmark)
            {
                return new BenchmarkAutomation(arguments.BenchmarkPath);
            }

            return arguments.TakesScreenshot ? new ScreenshotAutomation(arguments.ScreenshotPath) : (ViewerAutomation)new NoAutomation();
        }

        public virtual void Begin(IViewerHost host, double now)
        {
        }

        public virtual void ModelRead(double now)
        {
        }

        public virtual void SceneShown(double now)
        {
        }

        /// <summary>The model is shown and every element is clickable.</summary>
        public abstract void Ready(IViewerHost host, double now);

        /// <summary>The viewer shows a message instead of a model: no package, or one it cannot open.</summary>
        public abstract void Stopped(IViewerHost host, string message);

        public abstract void Update(IViewerHost host, double deltaSeconds);
    }
}
