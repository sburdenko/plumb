using System.Diagnostics;
using Plumb.Core.Geometry;

namespace Plumb.App.ViewModels;

/// <summary>
/// Words the user-facing message for missing geometry; the detail is shown in brackets when there is one.
/// </summary>
public static class GeometryWarning
{
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
