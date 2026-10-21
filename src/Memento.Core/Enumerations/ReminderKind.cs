namespace Memento.Core.Enumerations;

public enum ReminderKind
{
    /// <summary>The deadline is near: from the lead time before it until it is reached.</summary>
    Upcoming,

    /// <summary>The deadline has been reached.</summary>
    DueNow,
}
