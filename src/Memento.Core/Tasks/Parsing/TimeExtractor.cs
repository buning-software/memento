using System.Globalization;
using System.Text.RegularExpressions;

namespace Memento.Core.Tasks.Parsing;

internal static partial class TimeExtractor
{
    // "15:00", "at 15:00", "3pm", "3:30 pm", "om 15:00", "15u", "15 uur"
    [GeneratedRegex(
        @"(?:\b(?:at|om)\s+)?(?<![\d:])(?:(?<h>\d{1,2}):(?<m>\d{2})(?:\s?(?<ap>am|pm))?|(?<h>\d{1,2})\s?(?<ap>am|pm)|(?<h>\d{1,2})\s?(?:u|uur))\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex TimeRegex();

    /// <summary>Cuts out the first valid time of day; impossible ones ("13pm", "25:00") stay in the text.</summary>
    public static TimeOnly? Extract(ParseBuffer buffer)
    {
        foreach (Match match in TimeRegex().Matches(buffer.Text))
        {
            if (TryBuild(match) is { } time)
            {
                buffer.Cut(match);
                return time;
            }
        }
        return null;
    }

    private static TimeOnly? TryBuild(Match match)
    {
        int hours = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
        int minutes = match.Groups["m"].Success ? int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture) : 0;

        if (match.Groups["ap"].Success)
        {
            if (hours is < 1 or > 12)
                return null;
            bool pm = match.Groups["ap"].Value.Equals("pm", StringComparison.OrdinalIgnoreCase);
            hours = hours % 12 + (pm ? 12 : 0);
        }

        return hours <= 23 && minutes <= 59 ? new TimeOnly(hours, minutes) : null;
    }
}
