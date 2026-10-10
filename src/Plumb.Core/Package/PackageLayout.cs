using System;
using System.IO;

namespace Plumb.Core.Package
{
    /// <summary>
    /// File names inside a <c>.plumb</c> package folder.
    /// </summary>
    public static class PackageLayout
    {
        public const string Extension = ".plumb";
        public const string ManifestFile = "manifest.json";
        public const string DatabaseFile = "model.sqlite";
        public const string ElementIndexFile = "elements.json";
        public const string GeometryFile = "model.glb";
        public const int FormatVersion = 2;

        public static bool IsPackagePath(string path) =>
            Path.GetExtension(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Equals(Extension, StringComparison.OrdinalIgnoreCase);

        /// <summary>Where the package for <paramref name="sourceFile"/> goes, e.g. <c>Duplex.ifc</c> becomes <c>Duplex.plumb</c>.</summary>
        public static string PackagePathFor(string sourceFile, string outputDirectory) =>
            Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourceFile) + Extension);
    }
}
