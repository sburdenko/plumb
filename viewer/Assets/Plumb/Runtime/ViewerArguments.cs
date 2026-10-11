using System;
using System.Collections.Generic;

namespace Plumb.Viewer
{
    /// <summary>
    /// Command line of the viewer: <c>--package &lt;folder&gt;</c>, and for automated checks
    /// <c>--select &lt;GlobalId&gt;</c>, <c>--screenshot &lt;png&gt;</c>, which saves one frame and quits, and
    /// <c>--benchmark &lt;file&gt;</c>, which times loading and the frame rate, writes the result and quits.
    /// </summary>
    public sealed class ViewerArguments
    {
        private ViewerArguments(string packagePath, string selectId, string screenshotPath, string benchmarkPath)
        {
            PackagePath = packagePath;
            SelectId = selectId;
            ScreenshotPath = screenshotPath;
            BenchmarkPath = benchmarkPath;
        }

        public string PackagePath { get; }

        public string SelectId { get; }

        public string ScreenshotPath { get; }

        public string BenchmarkPath { get; }

        public bool HasPackage => !string.IsNullOrWhiteSpace(PackagePath);

        public bool TakesScreenshot => !string.IsNullOrWhiteSpace(ScreenshotPath);

        public bool RunsBenchmark => !string.IsNullOrWhiteSpace(BenchmarkPath);

        public static ViewerArguments Parse(IReadOnlyList<string> args)
        {
            return new ViewerArguments(
                ValueAfter(args, "--package"),
                ValueAfter(args, "--select"),
                ValueAfter(args, "--screenshot"),
                ValueAfter(args, "--benchmark"));
        }

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
