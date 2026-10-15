namespace Plumb.Viewer
{
    /// <summary>What an automated run asks of the running viewer.</summary>
    public interface IViewerHost
    {
        void CaptureScreenshot(string path);

        void WriteReport(string path, string text);

        void TurnCamera(float degrees);

        (int Meshes, long Triangles) CountGeometry();

        /// <summary>Turns vsync and the frame cap off, so frame times show what the model costs.</summary>
        void UncapFrameRate();

        void Quit();
    }
}
