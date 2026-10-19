namespace Memento.Core.Tasks.Parsing.Dates.Models;

/// <summary>A relative day keyword: how many days from today, and the time of day it implies, if any.</summary>
internal readonly record struct RelativeDay(int DaysFromToday, TimeOnly? ImpliedTime = null);
