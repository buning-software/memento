using Memento.Core.Tasks.Parsing.Dates;
using static Memento.Core.UnitTests.TestSupport.TestClock;
using static Memento.Core.UnitTests.TestSupport.TestParser;

namespace Memento.Core.UnitTests.Tasks.Parsing;

public class TaskParserRelativeDayTests
{
    [Theory]
    [InlineData("dentist today", "dentist", "2026-10-13T09:00")]
    [InlineData("dentist tomorrow", "dentist", "2026-10-14T09:00")]
    [InlineData("tandarts morgen", "tandarts", "2026-10-14T09:00")]
    [InlineData("dentist day after tomorrow", "dentist", "2026-10-15T09:00")]
    [InlineData("tandarts overmorgen", "tandarts", "2026-10-15T09:00")]
    [InlineData("run this morning", "run", "2026-10-13T09:00")]
    [InlineData("call this afternoon", "call", "2026-10-13T15:00")]
    [InlineData("dinner tonight", "dinner", "2026-10-13T19:00")]
    [InlineData("dinner this evening", "dinner", "2026-10-13T19:00")]
    [InlineData("eten vanavond", "eten", "2026-10-13T19:00")]
    [InlineData("dinner tonight 20:30", "dinner", "2026-10-13T20:30")]
    [InlineData("call Anna tomorrow 3pm", "call Anna", "2026-10-14T15:00")]
    [InlineData("bel Anna morgen om 15:00", "bel Anna", "2026-10-14T15:00")]
    [InlineData("run THIS   MORNING", "run", "2026-10-13T09:00")]
    public void Relative_days_resolve_against_now(string input, string title, string expected)
    {
        var task = Parse(input);

        Assert.Equal(title, task.Title);
        Assert.Equal(At(expected), task.Deadline);
    }

    [Theory]
    [InlineData("review wednesday", "2026-10-14T09:00")]
    [InlineData("report friday", "2026-10-16T09:00")]
    [InlineData("rapport vrijdag", "2026-10-16T09:00")]
    [InlineData("plan monday", "2026-10-19T09:00")]
    [InlineData("standup tuesday", "2026-10-20T09:00")]
    public void A_weekday_is_its_next_occurrence_and_today_means_next_week(string input, string expected)
    {
        Assert.Equal(At(expected), Parse(input).Deadline);
    }

    public static TheoryData<string> RelativeDayKeywords() => new(Vocabulary.RelativeDays.Keys);

    public static TheoryData<string> WeekdayKeywords() => new(Vocabulary.Weekdays.Keys);

    [Theory]
    [MemberData(nameof(RelativeDayKeywords))]
    public void Every_relative_day_keyword_is_recognised(string keyword)
    {
        var expected = Vocabulary.RelativeDays[keyword];

        var task = Parse("task " + keyword);

        Assert.Equal("task", task.Title);
        Assert.Equal(Now.Date.AddDays(expected.DaysFromToday).Add((expected.ImpliedTime ?? new TimeOnly(9, 0)).ToTimeSpan()), task.Deadline);
    }

    [Theory]
    [MemberData(nameof(WeekdayKeywords))]
    public void Every_weekday_keyword_is_recognised_and_lands_on_that_weekday(string keyword)
    {
        var task = Parse("task " + keyword);

        Assert.Equal("task", task.Title);
        Assert.Equal(Vocabulary.Weekdays[keyword], task.Deadline!.Value.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(task.Deadline.Value));
    }

    [Theory]
    [InlineData("wednesday", "2026-10-14")]
    [InlineData("thursday", "2026-10-15")]
    [InlineData("friday", "2026-10-16")]
    [InlineData("saturday", "2026-10-17")]
    [InlineData("sunday", "2026-10-18")]
    [InlineData("monday", "2026-10-19")]
    [InlineData("tuesday", "2026-10-20")]
    public void Weekdays_are_between_one_and_seven_days_ahead_and_never_today(string weekday, string expectedDate)
    {
        var deadline = Parse("x " + weekday).Deadline!.Value;

        Assert.Equal(At(expectedDate + "T09:00"), deadline);
        Assert.InRange((deadline.Date - Now.Date).Days, 1, 7);
    }

    [Theory]
    [InlineData("TOMORROW")]
    [InlineData("Tomorrow")]
    [InlineData("MORGEN")]
    [InlineData("OVERMORGEN")]
    [InlineData("Day After Tomorrow")]
    public void Keywords_are_matched_in_any_case(string keyword)
    {
        var task = Parse("x " + keyword);

        Assert.Equal("x", task.Title);
        Assert.NotNull(task.Deadline);
    }

    [Theory]
    [InlineData("tomorrow dentist", "dentist", "2026-10-14T09:00")]
    [InlineData("dentist tomorrow", "dentist", "2026-10-14T09:00")]
    [InlineData("tomorrow", "", "2026-10-14T09:00")]
    [InlineData("vanavond dinner", "dinner", "2026-10-13T19:00")]
    [InlineData("friday", "", "2026-10-16T09:00")]
    public void A_keyword_at_the_start_or_end_of_the_line_is_cut_out(string input, string title, string expected)
    {
        var task = Parse(input);

        Assert.Equal(title, task.Title);
        Assert.Equal(At(expected), task.Deadline);
    }

    [Theory]
    [InlineData("2026-10-31T10:00", "x tomorrow", "2026-11-01T09:00")]
    [InlineData("2026-12-31T10:00", "x tomorrow", "2027-01-01T09:00")]
    [InlineData("2028-02-28T10:00", "x day after tomorrow", "2028-03-01T09:00")]
    [InlineData("2028-02-28T10:00", "x tomorrow", "2028-02-29T09:00")]
    [InlineData("2026-02-28T10:00", "x tomorrow", "2026-03-01T09:00")]
    [InlineData("2026-12-30T10:00", "x friday", "2027-01-01T09:00")]
    public void Relative_days_roll_over_month_and_year_boundaries(string now, string input, string expected)
    {
        Assert.Equal(At(expected), Parse(input, now: At(now)).Deadline);
    }

    [Fact]
    public void A_time_of_day_in_the_past_does_not_move_a_relative_day()
    {
        var late = At("2026-10-13T23:30");

        Assert.Equal(At("2026-10-13T08:00"), Parse("x today 8:00", now: late).Deadline);
        Assert.Equal(At("2026-10-14T08:00"), Parse("x tomorrow 8:00", now: late).Deadline);
    }

    [Fact]
    public void Only_the_first_keyword_is_used_and_a_later_one_stays_in_the_title()
    {
        var task = Parse("tomorrow or friday");

        Assert.Equal(At("2026-10-14T09:00"), task.Deadline);
        Assert.Equal("or friday", task.Title);
    }

    [Theory]
    [InlineData("morgenstond")]
    [InlineData("todays")]
    [InlineData("mondays")]
    [InlineData("sundayish")]
    public void Words_that_only_start_with_a_keyword_are_ignored(string word)
    {
        var task = Parse("x " + word);

        Assert.Equal("x " + word, task.Title);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void An_explicit_time_beats_the_default_and_an_implied_time_beats_the_default()
    {
        var eightThirty = new TimeOnly(8, 30);

        Assert.Equal(At("2026-10-14T08:30"), Parse("x tomorrow", eightThirty).Deadline);
        Assert.Equal(At("2026-10-13T19:00"), Parse("x tonight", eightThirty).Deadline);
    }

    [Fact]
    public void Keywords_inside_words_are_ignored()
    {
        var task = Parse("Tomorrowland tickets");

        Assert.Equal("Tomorrowland tickets", task.Title);
        Assert.Null(task.Deadline);
    }

    [Fact]
    public void The_full_example_from_the_placeholder_parses()
    {
        var task = Parse("call Anna tomorrow 3pm #work");

        Assert.Equal("call Anna", task.Title);
        Assert.Equal(At("2026-10-14T15:00"), task.Deadline);
        Assert.Equal(new[] { "work" }, task.Tags);
    }
}
