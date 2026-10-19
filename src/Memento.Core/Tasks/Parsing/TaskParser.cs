using Memento.Core.Tasks.Parsing.Dates;
using Memento.Core.Tasks.Parsing.Models;

namespace Memento.Core.Tasks.Parsing;

/// <summary>
/// Splits one input line into title, deadline and #tags. Pure and stateless, so it can run on every keystroke.
/// Order matters: tags first, then the day, then the time; each part is cut out and the rest is the title.
/// </summary>
public static class TaskParser
{
    /// <param name="now">The moment relative words ("tomorrow") and a lone time are resolved against.</param>
    /// <param name="defaultTime">Time of day for a deadline that has a date but no time.</param>
    public static ParsedTask Parse(string input, DateTime now, TimeOnly defaultTime)
    {
        var buffer = new ParseBuffer(input);

        var tags = TagExtractor.Extract(buffer);
        var day = DateExtractor.Extract(buffer, DateOnly.FromDateTime(now));
        var time = TimeExtractor.Extract(buffer);

        DateTime? deadline = null;
        if (day is { } d)
            deadline = d.Date.ToDateTime(time ?? d.ImpliedTime ?? defaultTime);
        else if (time is { } t)
            deadline = DateOnly.FromDateTime(now).ToDateTime(t); // a time alone is today, even if passed: it shows as overdue

        string title = ParseBuffer.CollapseWhitespace(buffer.Text).Trim();
        return new ParsedTask(title, deadline, tags);
    }
}
