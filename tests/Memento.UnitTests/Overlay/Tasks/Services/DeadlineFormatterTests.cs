using Microsoft.Extensions.Time.Testing;
using Memento.Core.UnitTests.TestSupport;
using Memento.Overlay.Tasks.Services;

namespace Memento.UnitTests.Overlay.Tasks.Services;

public class DeadlineFormatterTests
{
    private readonly DeadlineFormatter _formatter = new(new FakeTimeProvider(TestClock.NowOffset));

    private static DeadlineFormatter FormatterAt(string now) =>
        new(new FakeTimeProvider(new DateTimeOffset(TestClock.At(now), TimeSpan.Zero)));

    [Theory]
    [InlineData(2026, 10, 13, 15, 0, "today 15:00")]
    [InlineData(2026, 10, 14, 9, 0, "tomorrow 09:00")]
    [InlineData(2026, 10, 16, 9, 0, "fri 16 oct 09:00")]
    [InlineData(2026, 10, 12, 18, 30, "mon 12 oct 18:30")]
    public void Format_names_today_and_tomorrow_and_otherwise_the_day(int year, int month, int day, int hour, int minute, string expected)
    {
        Assert.Equal(expected, _formatter.Format(new DateTime(year, month, day, hour, minute, 0)));
    }

    [Theory]
    [InlineData(2026, 10, 12, 23, 59, "mon 12 oct 23:59")]  // the last minute of yesterday
    [InlineData(2026, 10, 13, 0, 0, "today 00:00")]         // the first minute of today
    [InlineData(2026, 10, 13, 23, 59, "today 23:59")]       // the last minute of today
    [InlineData(2026, 10, 14, 0, 0, "tomorrow 00:00")]      // the first minute of tomorrow
    [InlineData(2026, 10, 14, 23, 59, "tomorrow 23:59")]    // the last minute of tomorrow
    [InlineData(2026, 10, 15, 0, 0, "thu 15 oct 00:00")]    // the day after tomorrow has no name of its own
    public void Format_switches_day_at_midnight(int year, int month, int day, int hour, int minute, string expected)
    {
        Assert.Equal(expected, _formatter.Format(new DateTime(year, month, day, hour, minute, 0)));
    }

    [Fact]
    public void Format_ignores_seconds()
    {
        Assert.Equal("today 15:07", _formatter.Format(new DateTime(2026, 10, 13, 15, 7, 59)));
    }

    [Theory]
    [InlineData("2026-12-31T10:00", "2027-01-01T09:00", "tomorrow 09:00")]
    [InlineData("2026-12-31T10:00", "2027-01-02T09:00", "sat 2 jan 09:00")]
    [InlineData("2026-12-31T10:00", "2026-12-31T23:59", "today 23:59")]
    [InlineData("2027-01-01T10:00", "2026-12-31T23:59", "thu 31 dec 23:59")]
    [InlineData("2026-10-31T10:00", "2026-11-01T09:00", "tomorrow 09:00")]
    [InlineData("2028-02-28T10:00", "2028-02-29T09:00", "tomorrow 09:00")]
    [InlineData("2028-02-28T10:00", "2028-03-01T09:00", "wed 1 mar 09:00")]
    public void Format_follows_month_and_year_boundaries(string now, string deadline, string expected)
    {
        Assert.Equal(expected, FormatterAt(now).Format(TestClock.At(deadline)));
    }

    [Fact]
    public void Format_does_not_show_the_year_of_a_far_away_deadline()
    {
        Assert.Equal("fri 1 jan 09:00", _formatter.Format(new DateTime(2027, 1, 1, 9, 0, 0)));
    }

    [Fact]
    public void Format_pads_the_time_but_not_the_day_of_the_month()
    {
        Assert.Equal("sat 3 oct 07:05", FormatterAt("2026-09-30T10:00").Format(new DateTime(2026, 10, 3, 7, 5, 0)));
    }

    [Fact]
    public void A_deadline_before_now_is_overdue()
    {
        Assert.True(_formatter.IsOverdue(new DateTime(2026, 10, 13, 9, 59, 0)));
        Assert.False(_formatter.IsOverdue(new DateTime(2026, 10, 13, 10, 1, 0)));
    }

    [Fact]
    public void A_deadline_exactly_now_is_not_overdue_yet()
    {
        Assert.False(_formatter.IsOverdue(TestClock.Now));
    }

    [Fact]
    public void A_deadline_one_second_before_now_is_overdue()
    {
        Assert.True(_formatter.IsOverdue(TestClock.Now.AddSeconds(-1)));
        Assert.False(_formatter.IsOverdue(TestClock.Now.AddSeconds(1)));
    }

    [Theory]
    [InlineData("2026-10-12T23:59", true)]    // yesterday
    [InlineData("2026-10-13T00:00", true)]    // earlier today
    [InlineData("2026-10-13T23:59", false)]   // later today
    [InlineData("2026-10-14T00:00", false)]   // tomorrow
    [InlineData("2026-10-15T09:00", false)]   // day after tomorrow
    [InlineData("2026-10-01T09:00", true)]    // last month
    [InlineData("2025-10-13T10:00", true)]    // last year
    public void Overdue_is_decided_by_the_exact_moment_not_by_the_day(string deadline, bool expected)
    {
        Assert.Equal(expected, _formatter.IsOverdue(TestClock.At(deadline)));
    }
}
