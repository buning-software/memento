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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \t")]
    public void Empty_or_whitespace_input_gives_an_empty_task(string input)
    {
        var task = Parse(input);

        Assert.Equal("", task.Title);
        Assert.Null(task.Deadline);
        Assert.Empty(task.Tags);
    }

    [Fact]
    public void A_lone_hash_is_not_a_tag_and_stays_in_the_title()
    {
        var task = Parse("issue # later");

        Assert.Equal("issue # later", task.Title);
        Assert.Empty(task.Tags);
    }

    [Theory]
    [InlineData("call #Work", "work")]
    [InlineData("call #WORK", "work")]
    [InlineData("call #2026", "2026")]
    [InlineData("call #q3_plan", "q3_plan")]
    [InlineData("call #home,", "home")]
    [InlineData("call (#home)", "home")]
    public void A_tag_is_the_word_after_the_hash_lower_cased(string input, string expectedTag)
    {
        var task = Parse(input);

        Assert.Equal(new[] { expectedTag }, task.Tags);
        Assert.StartsWith("call", task.Title);
        Assert.DoesNotContain("#", task.Title);
    }

    [Fact]
    public void A_tag_in_the_middle_leaves_the_words_around_it_apart()
    {
        var task = Parse("buy #shop milk #home now");

        Assert.Equal("buy milk now", task.Title);
        Assert.Equal(new[] { "shop", "home" }, task.Tags);
    }

    [Theory]
    [InlineData("pay rent 2026-1-4", "2026-01-04T09:00")]
    [InlineData("pay rent 2026-10-4", "2026-10-04T09:00")]
    [InlineData("pay rent 2026-1-14", "2026-01-14T09:00")]
    [InlineData("pay rent 4-1-2027", "2027-01-04T09:00")]
    [InlineData("pay rent 04-01-2027", "2027-01-04T09:00")]
    [InlineData("pay rent 2028-02-29", "2028-02-29T09:00")]
    [InlineData("pay rent 29-02-2028", "2028-02-29T09:00")]
    [InlineData("pay rent 31-12-2026", "2026-12-31T09:00")]
    public void Dates_with_single_digit_parts_and_leap_days_are_read(string input, string expected)
    {
        var task = Parse(input);

        Assert.Equal("pay rent", task.Title);
        Assert.Equal(At(expected), task.Deadline);
    }

    [Theory]
    [InlineData("party 2026-02-29")]
    [InlineData("party 29-02-2026")]
    [InlineData("party 2026-13-01")]
    [InlineData("party 2026-04-31")]
    [InlineData("party 00-10-2026")]
    public void An_impossible_date_in_either_notation_stays_in_the_title(string input)
    {
        var task = Parse(input);

        Assert.Equal(input, task.Title);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void An_absolute_date_wins_over_a_keyword()
    {
        var task = Parse("dentist tomorrow 20-10-2026");

        Assert.Equal(At("2026-10-20T09:00"), task.Deadline);
        Assert.Equal("dentist tomorrow", task.Title);
    }

    [Fact]
    public void A_keyword_is_used_when_the_absolute_date_is_impossible()
    {
        var task = Parse("dentist tomorrow 31-02-2026");

        Assert.Equal(At("2026-10-14T09:00"), task.Deadline);
        Assert.Equal("dentist 31-02-2026", task.Title);
    }

    [Theory]
    [InlineData("night 00:00", "00:00")]
    [InlineData("night 0:00", "00:00")]
    [InlineData("late 23:59", "23:59")]
    [InlineData("late 23u", "23:00")]
    [InlineData("noon 12:30pm", "12:30")]
    [InlineData("night 12:30am", "00:30")]
    [InlineData("early 1am", "01:00")]
    [InlineData("late 11pm", "23:00")]
    public void Boundary_times_are_read(string input, string time)
    {
        var task = Parse(input);

        Assert.Equal(At("2026-10-13T" + time), task.Deadline);
        Assert.Equal(input[..input.IndexOf(' ')], task.Title);
    }

    [Theory]
    [InlineData("invalid 0pm")]
    [InlineData("invalid 13am")]
    [InlineData("invalid 24:00")]
    [InlineData("invalid 12:60")]
    [InlineData("invalid 24u")]
    public void Times_just_outside_the_clock_stay_in_the_title(string input)
    {
        var task = Parse(input);

        Assert.Equal(input, task.Title);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void The_first_valid_time_wins_and_a_second_one_stays_in_the_title()
    {
        var task = Parse("call 9:00 or 17:00");

        Assert.Equal(At("2026-10-13T09:00"), task.Deadline);
        Assert.Equal("call or 17:00", task.Title);
    }

    [Fact]
    public void An_impossible_time_is_skipped_for_the_next_valid_one()
    {
        var task = Parse("call 25:00 or 17:00");

        Assert.Equal(At("2026-10-13T17:00"), task.Deadline);
        Assert.Equal("call 25:00 or", task.Title);
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
