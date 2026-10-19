using Memento.Core.Tasks.Parsing.Dates.Models;

namespace Memento.Core.Tasks.Parsing.Dates;

/// <summary>
/// Every day keyword the parser understands: English first, Dutch equivalents after.
/// Adding a language means adding entries here only; <see cref="DateExtractor"/> builds its regex from these keys.
/// A space in a key matches any run of whitespace.
/// </summary>
internal static class Vocabulary
{
    private static readonly TimeOnly Morning = new(9, 0);
    private static readonly TimeOnly Afternoon = new(15, 0);
    private static readonly TimeOnly Evening = new(19, 0);

    public static readonly IReadOnlyDictionary<string, RelativeDay> RelativeDays =
        new Dictionary<string, RelativeDay>(StringComparer.OrdinalIgnoreCase)
        {
            ["today"] = new(0),
            ["vandaag"] = new(0),
            ["tomorrow"] = new(1),
            ["morgen"] = new(1),
            ["day after tomorrow"] = new(2),
            ["overmorgen"] = new(2),
            ["this morning"] = new(0, Morning),
            ["vanochtend"] = new(0, Morning),
            ["this afternoon"] = new(0, Afternoon),
            ["vanmiddag"] = new(0, Afternoon),
            ["tonight"] = new(0, Evening),
            ["this evening"] = new(0, Evening),
            ["vanavond"] = new(0, Evening),
        };

    public static readonly IReadOnlyDictionary<string, DayOfWeek> Weekdays =
        new Dictionary<string, DayOfWeek>(StringComparer.OrdinalIgnoreCase)
        {
            ["sunday"] = DayOfWeek.Sunday,
            ["monday"] = DayOfWeek.Monday,
            ["tuesday"] = DayOfWeek.Tuesday,
            ["wednesday"] = DayOfWeek.Wednesday,
            ["thursday"] = DayOfWeek.Thursday,
            ["friday"] = DayOfWeek.Friday,
            ["saturday"] = DayOfWeek.Saturday,
            ["zondag"] = DayOfWeek.Sunday,
            ["maandag"] = DayOfWeek.Monday,
            ["dinsdag"] = DayOfWeek.Tuesday,
            ["woensdag"] = DayOfWeek.Wednesday,
            ["donderdag"] = DayOfWeek.Thursday,
            ["vrijdag"] = DayOfWeek.Friday,
            ["zaterdag"] = DayOfWeek.Saturday,
        };
}
