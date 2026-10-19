using Memento.Core.Tasks.Parsing;
using Memento.Core.Tasks.Parsing.Models;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>Parses at <see cref="TestClock.Now"/> with 09:00 as the default time unless another is given.</summary>
internal static class TestParser
{
    public static ParsedTask Parse(string input, TimeOnly? defaultTime = null) =>
        TaskParser.Parse(input, TestClock.Now, defaultTime ?? new TimeOnly(9, 0));
}
