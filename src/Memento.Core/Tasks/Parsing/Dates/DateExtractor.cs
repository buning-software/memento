using System.Globalization;
using System.Text.RegularExpressions;
using Memento.Core.Tasks.Parsing;
using Memento.Core.Tasks.Parsing.Dates.Models;

namespace Memento.Core.Tasks.Parsing.Dates;

internal static partial class DateExtractor
{
    // 14-10-2026 (day-month-year)
    [GeneratedRegex(@"(?<![\w-])\d{1,2}-\d{1,2}-\d{4}(?![\w-])")]
    private static partial Regex DayMonthYearRegex();

    // 2026-10-14 (ISO)
    [GeneratedRegex(@"(?<![\w-])\d{4}-\d{1,2}-\d{1,2}(?![\w-])")]
    private static partial Regex IsoDateRegex();

    private static readonly Regex KeywordRegex = BuildKeywordRegex();

    /// <summary>Cuts out the first date it recognises: an absolute date wins over a keyword.</summary>
    public static DayMatch? Extract(ParseBuffer buffer, DateOnly today) =>
        TryAbsolute(buffer, DayMonthYearRegex(), "d-M-yyyy")
        ?? TryAbsolute(buffer, IsoDateRegex(), "yyyy-M-d")
        ?? TryKeyword(buffer, today);

    private static DayMatch? TryAbsolute(ParseBuffer buffer, Regex regex, string format)
    {
        var match = regex.Match(buffer.Text);
        if (!match.Success ||
            !DateOnly.TryParseExact(match.Value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        buffer.Cut(match);
        return new DayMatch(date, ImpliedTime: null);
    }

    private static DayMatch? TryKeyword(ParseBuffer buffer, DateOnly today)
    {
        var match = KeywordRegex.Match(buffer.Text);
        if (!match.Success)
            return null;

        buffer.Cut(match);
        string keyword = ParseBuffer.CollapseWhitespace(match.Value);

        if (Vocabulary.RelativeDays.TryGetValue(keyword, out var relative))
            return new DayMatch(today.AddDays(relative.DaysFromToday), relative.ImpliedTime);

        // A weekday is its next occurrence; today's own weekday means next week.
        int days = ((int)Vocabulary.Weekdays[keyword] - (int)today.DayOfWeek + 7) % 7;
        return new DayMatch(today.AddDays(days == 0 ? 7 : days), ImpliedTime: null);
    }

    private static Regex BuildKeywordRegex()
    {
        // Longest first, so "day after tomorrow" wins over "tomorrow"; a space in a keyword matches any whitespace.
        var alternatives = Vocabulary.RelativeDays.Keys
            .Concat(Vocabulary.Weekdays.Keys)
            .OrderByDescending(keyword => keyword.Length)
            .Select(keyword => Regex.Escape(keyword).Replace(@"\ ", @"\s+"));

        return new Regex(@"\b(?:" + string.Join('|', alternatives) + @")\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
