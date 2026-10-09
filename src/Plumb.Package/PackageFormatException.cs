namespace Plumb.Package;

/// <summary>
/// The folder exists but is not a package this version can read.
/// </summary>
public sealed class PackageFormatException(string message, Exception? innerException = null)
    : Exception(message, innerException);
