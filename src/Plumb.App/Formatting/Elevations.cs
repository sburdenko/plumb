using System.Globalization;

namespace Plumb.App.Formatting;

public static class Elevations
{
    private const char Minus = '−';

    /// <summary>"+6.100", "±0.000", "−1.220": metres to three decimals, the way drawings label levels.</summary>
    public static string Format(double metres)
    {
        var rounded = Math.Round(metres, 3, MidpointRounding.AwayFromZero);
        var digits = Math.Abs(rounded).ToString("0.000", CultureInfo.InvariantCulture);
        return rounded switch
        {
            > 0 => "+" + digits,
            < 0 => Minus + digits,
            _ => "±" + digits,
        };
    }
}
