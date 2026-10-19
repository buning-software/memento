namespace Memento.Core.Tasks.Parsing.Models;

/// <summary>What the parser read from one input line.</summary>
public sealed record ParsedTask(string Title, DateTime? Deadline, IReadOnlyList<string> Tags);
