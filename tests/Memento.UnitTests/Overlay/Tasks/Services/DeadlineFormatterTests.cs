using Microsoft.Extensions.Time.Testing;
using Memento.Core.UnitTests.TestSupport;
using Memento.Overlay.Tasks.Services;

namespace Memento.UnitTests.Overlay.Tasks.Services;

public class DeadlineFormatterTests
{
    private readonly DeadlineFormatter _formatter = new(new FakeTimeProvider(TestClock.NowOffset));

    [Theory]
    [InlineData(2026, 10, 13, 15, 0, "today 15:00")]
    [InlineData(2026, 10, 14, 9, 0, "tomorrow 09:00")]
    [InlineData(2026, 10, 16, 9, 0, "fri 16 oct 09:00")]
    [InlineData(2026, 10, 12, 18, 30, "mon 12 oct 18:30")]
    public void Format_names_today_and_tomorrow_and_otherwise_the_day(int year, int month, int day, int hour, int minute, string expected)
    {
        Assert.Equal(expected, _formatter.Format(new DateTime(year, month, day, hour, minute, 0)));
    }

    [Fact]
    public void A_deadline_before_now_is_overdue()
    {
        Assert.True(_formatter.IsOverdue(new DateTime(2026, 10, 13, 9, 59, 0)));
        Assert.False(_formatter.IsOverdue(new DateTime(2026, 10, 13, 10, 1, 0)));
    }
}
