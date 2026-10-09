using System.Globalization;
using Xbim.Ifc4.Interfaces;

namespace Plumb.Ifc;

internal static class IfcValueFormatter
{
    // Ten significant digits hide binary floating-point tails such as 17.38299999999997 from Revit exports.
    private const string DoubleFormat = "G10";

    public static string? Format(IIfcValue? value) => value?.Value switch
    {
        bool flag => flag ? "true" : "false",
        double number => number.ToString(DoubleFormat, CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        var raw => raw?.ToString(),
    };
}
