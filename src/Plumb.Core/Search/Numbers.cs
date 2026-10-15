using System.Globalization;

namespace Plumb.Core.Search
{
    /// <summary>What counts as a number in a query and in a property value: invariant culture, finite.</summary>
    internal static class Numbers
    {
        public static bool TryParseFinite(string text, out double number) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            && !double.IsNaN(number)
            && !double.IsInfinity(number);
    }
}
