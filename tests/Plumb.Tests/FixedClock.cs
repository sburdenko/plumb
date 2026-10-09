namespace Plumb.Tests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone { get; } =
        TimeZoneInfo.CreateCustomTimeZone("Fixed", now.Offset, "Fixed", "Fixed");
}
