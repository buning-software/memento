using System.Text.Json;
using Memento.Core.Settings.Converters;

namespace Memento.Core.UnitTests.Settings.Converters;

public class HourMinuteJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new HourMinuteJsonConverter() } };

    private static readonly TimeOnly Default = new(9, 0);

    private sealed record Holder(TimeOnly Before, TimeOnly Time, string After);

    [Theory]
    [InlineData(0, 0, "\"00:00\"")]
    [InlineData(0, 5, "\"00:05\"")]
    [InlineData(9, 30, "\"09:30\"")]
    [InlineData(23, 59, "\"23:59\"")]
    public void A_time_is_written_as_two_digit_hours_and_minutes(int hours, int minutes, string expected)
    {
        Assert.Equal(expected, JsonSerializer.Serialize(new TimeOnly(hours, minutes), Options));
    }

    [Fact]
    public void Seconds_are_not_written()
    {
        Assert.Equal("\"09:30\"", JsonSerializer.Serialize(new TimeOnly(9, 30, 45), Options));
    }

    [Theory]
    [InlineData("\"00:00\"", 0, 0)]
    [InlineData("\"08:30\"", 8, 30)]
    [InlineData("\"23:59\"", 23, 59)]
    public void A_valid_string_is_read(string json, int hours, int minutes)
    {
        Assert.Equal(new TimeOnly(hours, minutes), JsonSerializer.Deserialize<TimeOnly>(json, Options));
    }

    [Fact]
    public void A_written_time_is_read_back_unchanged()
    {
        var time = new TimeOnly(17, 45);

        var roundTripped = JsonSerializer.Deserialize<TimeOnly>(JsonSerializer.Serialize(time, Options), Options);

        Assert.Equal(time, roundTripped);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"nine\"")]
    [InlineData("\"25:00\"")]
    [InlineData("\"10:75\"")]
    [InlineData("\"09:30:00\"")]
    [InlineData("\"9.30\"")]
    public void An_unreadable_string_gives_the_default(string json)
    {
        Assert.Equal(Default, JsonSerializer.Deserialize<TimeOnly>(json, Options));
    }

    [Theory]
    [InlineData("930")]
    [InlineData("9.5")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void A_value_that_is_not_a_string_gives_the_default(string json)
    {
        Assert.Equal(Default, JsonSerializer.Deserialize<TimeOnly>(json, Options));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "Hour": 9, "Minute": [30], "Nested": { "a": [1, 2] } }""")]
    [InlineData("[]")]
    [InlineData("""["09:30", { "x": 1 }]""")]
    public void An_object_or_array_is_skipped_whole_and_gives_the_default(string json)
    {
        Assert.Equal(Default, JsonSerializer.Deserialize<TimeOnly>(json, Options));
    }

    [Theory]
    [InlineData("{ \"Hour\": 9, \"Minute\": [30] }")]
    [InlineData("[\"09:30\"]")]
    [InlineData("930")]
    [InlineData("\"junk\"")]
    public void A_bad_value_does_not_disturb_the_properties_around_it(string badValue)
    {
        string json = $$"""{ "Before": "07:15", "Time": {{badValue}}, "After": "kept" }""";

        var holder = JsonSerializer.Deserialize<Holder>(json, Options);

        Assert.Equal(new TimeOnly(7, 15), holder?.Before);
        Assert.Equal(Default, holder?.Time);
        Assert.Equal("kept", holder?.After);
    }
}
