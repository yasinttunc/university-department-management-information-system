namespace UniversityDepartmentMIS.Core.Tests;

/// <summary>A clock fixed at noon UTC on 1 June of the given year.</summary>
internal sealed class TestClock(int year) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(year, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
