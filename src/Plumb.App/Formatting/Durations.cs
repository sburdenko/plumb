using System.Globalization;

namespace Plumb.App.Formatting;

public static class Durations
{
    /// <summary>"15 ms" under a second, "1.2 s" from one second on.</summary>
    public static string Format(TimeSpan duration) =>
        duration.TotalSeconds < 1
            ? string.Format(CultureInfo.InvariantCulture, "{0:0} ms", duration.TotalMilliseconds)
            : string.Format(CultureInfo.InvariantCulture, "{0:0.0} s", duration.TotalSeconds);
}
