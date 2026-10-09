namespace Plumb.Core.Import
{
    public enum ImportError
    {
        /// <summary>The path does not point to an existing file.</summary>
        FileNotFound,

        /// <summary>The file is not an IFC STEP file (wrong extension or no ISO-10303-21 header).</summary>
        NotIfc,

        /// <summary>The file looks like IFC but could not be parsed or has no IfcProject.</summary>
        ParseFailed,

        /// <summary>The import was cancelled by the caller.</summary>
        Cancelled,

        /// <summary>A file or folder could not be read or written (permissions, locks, disk errors).</summary>
        IoError,

        /// <summary>The folder is not a <c>.plumb</c> package this version can open.</summary>
        PackageInvalid,
    }
}
