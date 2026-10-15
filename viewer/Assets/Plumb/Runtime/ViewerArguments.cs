using System;
using System.Collections.Generic;
using System.Globalization;

namespace Plumb.Viewer
{
    /// <summary>
    /// Command line of the viewer: <c>--package &lt;folder&gt;</c>; <c>--link &lt;port&gt;</c> when Plumb started it,
    /// with the token in the <c>PLUMB_LINK_TOKEN</c> environment variable; and for automated checks
    /// <c>--select &lt;GlobalId&gt;</c>, <c>--screenshot &lt;png&gt;</c>, which saves one frame and quits, and
    /// <c>--benchmark &lt;file&gt;</c>, which times loading and the frame rate, writes the result and quits.
    /// </summary>
    public sealed class ViewerArguments
    {
        public const string LinkTokenVariable = "PLUMB_LINK_TOKEN";

        private ViewerArguments(string packagePath, string selectId, string screenshotPath, string benchmarkPath, int? linkPort, string linkToken)
        {
            PackagePath = packagePath;
            SelectId = selectId;
            ScreenshotPath = screenshotPath;
            BenchmarkPath = benchmarkPath;
            LinkPort = linkPort;
            LinkToken = linkToken;
        }

        public string PackagePath { get; }

        public string SelectId { get; }

        public string ScreenshotPath { get; }

        public string BenchmarkPath { get; }

        /// <summary>The port Plumb listens on, or null when the viewer was not started with both a port and a token.</summary>
        public int? LinkPort { get; }

        public string LinkToken { get; }

        public bool HasPackage => !string.IsNullOrWhiteSpace(PackagePath);

        public bool TakesScreenshot => !string.IsNullOrWhiteSpace(ScreenshotPath);

        public bool RunsBenchmark => !string.IsNullOrWhiteSpace(BenchmarkPath);

        /// <param name="linkToken">The value of <see cref="LinkTokenVariable"/>.</param>
        public static ViewerArguments Parse(IReadOnlyList<string> args, string linkToken = null)
        {
            var linkPort = !string.IsNullOrEmpty(linkToken) && TryParsePort(ValueAfter(args, "--link"), out var port) ? port : (int?)null;
            return new ViewerArguments(
                ValueAfter(args, "--package"),
                ValueAfter(args, "--select"),
                ValueAfter(args, "--screenshot"),
                ValueAfter(args, "--benchmark"),
                linkPort,
                linkPort == null ? null : linkToken);
        }

        private static bool TryParsePort(string text, out int port) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out port) && port > 0 && port <= 65535;

        private static string ValueAfter(IReadOnlyList<string> args, string name)
        {
            for (var i = 0; i < args.Count - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
