using System.Globalization;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>The fixed "now" of every test: Tuesday 2026-10-13 10:00 (local = UTC in FakeTimeProvider).</summary>
internal static class TestClock
{
    public static readonly DateTime Now = new(2026, 10, 13, 10, 0, 0);

    /// <summary><see cref="Now"/> as the start time of a FakeTimeProvider.</summary>
    public static readonly DateTimeOffset NowOffset = new(Now, TimeSpan.Zero);

    /// <summary>A local time written as "yyyy-MM-ddTHH:mm".</summary>
    public static DateTime At(string isoDateTime) =>
        DateTime.ParseExact(isoDateTime, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
}
