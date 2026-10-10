using System.Diagnostics;
using Plumb.Core.Geometry;

namespace Plumb.App.Screens.Model.Viewport;

/// <summary>
/// Words the user-facing message for missing geometry; the detail is shown in brackets when there is one.
/// </summary>
public static class GeometryWarning
{
    /// <summary>The explanation under the "3D geometry was not built" heading in the viewport.</summary>
    public static string Reason(GeometryState.NotBuilt notBuilt) => notBuilt.Error switch
    {
        GeometryError.ConverterMissing => "IfcConvert was not found next to the app. The tree and properties are open and complete.",
        GeometryError.ConverterFailed => "IfcConvert could not convert this file. The tree and properties are open and complete.",
        GeometryError.Timeout => "IfcConvert took too long and was stopped. The tree and properties are open and complete.",
        GeometryError.FileMissing => "This package was saved without model.glb. Import the IFC file again to add it.",
        GeometryError.Unknown => "The geometry could not be built. The tree and properties are open and complete.",
        _ => throw new UnreachableException(),
    };

    public static string For(GeometryState.NotBuilt notBuilt)
    {
        var message = notBuilt.Error switch
        {
            GeometryError.ConverterMissing => "3D geometry was not built: IfcConvert is not installed next to Plumb.",
            GeometryError.ConverterFailed => "3D geometry was not built: IfcConvert could not convert this file.",
            GeometryError.Timeout => "3D geometry was not built: IfcConvert took too long and was stopped.",
            GeometryError.FileMissing => "This package has no 3D geometry.",
            GeometryError.Unknown => "3D geometry was not built.",
            _ => throw new UnreachableException(),
        };

        return notBuilt.Error is GeometryError.ConverterFailed or GeometryError.Unknown && notBuilt.Detail.Length > 0
            ? $"{message} ({notBuilt.Detail})"
            : message;
    }
}
