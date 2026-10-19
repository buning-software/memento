using Memento.Core.Tasks.Models;
using static Memento.Core.UnitTests.TestSupport.TestClock;
using static Memento.Core.UnitTests.TestSupport.TestParser;

namespace Memento.Core.UnitTests.Tasks.Parsing;

public class TaskParserTests
{
    [Fact]
    public void Plain_text_becomes_the_title()
    {
        var task = Parse("buy milk");

        Assert.Equal("buy milk", task.Title);
        Assert.Null(task.Deadline);
        Assert.Empty(task.Tags);
    }

    [Fact]
    public void Tags_are_cut_out_lower_cased_and_deduplicated()
    {
        var task = Parse("plan trip #Travel #travel #fun");

        Assert.Equal("plan trip", task.Title);
        Assert.Equal(new[] { "travel", "fun" }, task.Tags);
    }

    [Fact]
    public void Only_tags_leave_an_empty_title()
    {
        Assert.Equal("", Parse("#a #b").Title);
    }

    [Theory]
    [InlineData("pay rent 14-10-2026", "2026-10-14T09:00")]
    [InlineData("pay rent 2026-10-14", "2026-10-14T09:00")]
    [InlineData("pay rent 2026-10-14 16:45", "2026-10-14T16:45")]
    [InlineData("pay rent 14-10-2026 at 4pm", "2026-10-14T16:00")]
    public void Absolute_dates_are_read_day_first_or_iso(string input, string expected)
    {
        var task = Parse(input);

        Assert.Equal("pay rent", task.Title);
        Assert.Equal(At(expected), task.Deadline);
    }

    [Fact]
    public void A_date_without_a_time_uses_the_default_time()
    {
        Assert.Equal(At("2026-10-14T08:30"), Parse("x 14-10-2026", new TimeOnly(8, 30)).Deadline);
    }

    [Fact]
    public void An_impossible_date_stays_in_the_title()
    {
        var task = Parse("party 31-02-2026");

        Assert.Equal("party 31-02-2026", task.Title);
        Assert.Null(task.Deadline);
    }

    [Theory]
    [InlineData("gym 7am", "gym", "07:00")]
    [InlineData("call 15:00", "call", "15:00")]
    [InlineData("call at 3:30 pm", "call", "15:30")]
    [InlineData("bel om 15u", "bel", "15:00")]
    [InlineData("bel 15 uur", "bel", "15:00")]
    [InlineData("lunch 12pm", "lunch", "12:00")]
    [InlineData("late 12am", "late", "00:00")]
    public void A_time_alone_means_today_even_when_it_has_passed(string input, string title, string time)
    {
        var task = Parse(input);

        Assert.Equal(title, task.Title);
        Assert.Equal(At("2026-10-13T" + time), task.Deadline);
    }

    [Theory]
    [InlineData("invalid 13pm")]
    [InlineData("invalid 25:00")]
    [InlineData("invalid 10:75")]
    public void Impossible_times_stay_in_the_title(string input)
    {
        var task = Parse(input);

        Assert.Equal(input, task.Title);
        Assert.Null(task.Deadline);
    }

    [Theory]
    [InlineData("buy 2 apples")]
    [InlineData("room 101")]
    [InlineData("version 1-2-3")]
    public void Numbers_and_words_that_only_look_like_dates_stay_in_the_title(string input)
    {
        var task = Parse(input);

        Assert.Equal(input, task.Title);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void Whitespace_left_behind_is_collapsed()
    {
        Assert.Equal("call Anna later", Parse("  call   Anna \t 15:00 later ").Title);
    }

    [Fact]
    public void A_rebuilt_edit_text_parses_back_to_the_same_task()
    {
        var stored = new TaskItem("1", "pay rent", At("2026-10-14T16:45"), ["home"], Now);

        var reparsed = Parse(stored.EditText);

        Assert.Equal(stored.Title, reparsed.Title);
        Assert.Equal(stored.Deadline, reparsed.Deadline);
        Assert.Equal(stored.Tags, reparsed.Tags);
    }
}
