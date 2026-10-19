namespace Memento.Core.Tasks.Parsing.Dates.Models;

/// <summary>A recognised day, plus the time of day its keyword implies ("tonight" = 19:00), if any.</summary>
internal readonly record struct DayMatch(DateOnly Date, TimeOnly? ImpliedTime);
