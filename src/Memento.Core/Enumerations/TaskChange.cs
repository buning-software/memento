namespace Memento.Core.Enumerations;

public enum TaskChange
{
    Saved,

    /// <summary>The edited text equals the stored text: nothing was written.</summary>
    Unchanged,

    /// <summary>The line holds only a deadline and/or tags: a task needs a title, so nothing was written.</summary>
    MissingTitle,
}
