using System.Globalization;

namespace Memento.Overlay.Tasks.Services;

/// <summary>Deadline text for the list and the preview: "today 15:00", "tomorrow 09:00", "fri 16 oct 09:00".</summary>
public sealed class DeadlineFormatter(TimeProvider time)
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-GB");

    public string Format(DateTime deadline)
    {
        var today = time.GetLocalNow().Date;
        string day = deadline.Date == today ? "today"
            : deadline.Date == today.AddDays(1) ? "tomorrow"
            : deadline.ToString("ddd d MMM", DisplayCulture).ToLowerInvariant();
        return $"{day} {deadline.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }

    public bool IsOverdue(DateTime deadline) => deadline < time.GetLocalNow().DateTime;
}
