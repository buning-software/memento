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
