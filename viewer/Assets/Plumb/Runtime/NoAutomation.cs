namespace Plumb.Viewer
{
    /// <summary>An ordinary run: the viewer is only looked at.</summary>
    public sealed class NoAutomation : ViewerAutomation
    {
        public override void Ready(IViewerHost host, double now)
        {
        }

        public override void Stopped(IViewerHost host, string message)
        {
        }

        public override void Update(IViewerHost host, double deltaSeconds)
        {
        }
    }
}
