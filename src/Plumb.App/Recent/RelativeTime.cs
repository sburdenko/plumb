using System.Globalization;

namespace Plumb.App.Recent;

/// <summary>Short descriptions of past moments: "5 min ago", "yesterday", "12 Sep".</summary>
public static class RelativeTime
{
    private const int DaysShownAsCount = 7;

    /// <param name="now">The current time in the viewer's time zone; calendar days are counted in it.</param>
    public static string Format(DateTimeOffset when, DateTimeOffset now)
    {
        var local = when.ToOffset(now.Offset);
        var elapsed = now - local;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes} min ago";
        }

        var days = (now.Date - local.Date).Days;
        return days switch
        {
            0 => $"{(int)elapsed.TotalHours} h ago",
            1 => "yesterday",
            < DaysShownAsCount => $"{days} days ago",
            _ => local.ToString(local.Year == now.Year ? "d MMM" : "d MMM yyyy", CultureInfo.InvariantCulture),
        };
    }
}
