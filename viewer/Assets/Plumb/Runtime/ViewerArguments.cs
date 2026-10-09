using System;
using System.Collections.Generic;

namespace Plumb.Viewer
{
    /// <summary>
    /// Command line of the viewer: <c>--package &lt;folder&gt;</c>, and for automated checks
    /// <c>--select &lt;GlobalId&gt;</c> and <c>--screenshot &lt;png&gt;</c>, which saves one frame and quits.
    /// </summary>
    public sealed class ViewerArguments
    {
        private ViewerArguments(string packagePath, string selectId, string screenshotPath)
        {
            PackagePath = packagePath;
            SelectId = selectId;
            ScreenshotPath = screenshotPath;
        }

        public string PackagePath { get; }

        public string SelectId { get; }

        public string ScreenshotPath { get; }

        public bool HasPackage => !string.IsNullOrWhiteSpace(PackagePath);

        public bool TakesScreenshot => !string.IsNullOrWhiteSpace(ScreenshotPath);

        public static ViewerArguments Parse(IReadOnlyList<string> args)
        {
            return new ViewerArguments(
                ValueAfter(args, "--package"),
                ValueAfter(args, "--select"),
                ValueAfter(args, "--screenshot"));
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
