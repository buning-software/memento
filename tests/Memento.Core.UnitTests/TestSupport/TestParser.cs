using Memento.Core.Tasks.Parsing;
using Memento.Core.Tasks.Parsing.Models;

namespace Memento.Core.UnitTests.TestSupport;

/// <summary>Parses at <see cref="TestClock.Now"/> (unless another "now" is given) with 09:00 as the default time unless another is given.</summary>
internal static class TestParser
{
    public static ParsedTask Parse(string input, TimeOnly? defaultTime = null, DateTime? now = null) =>
        TaskParser.Parse(input, now ?? TestClock.Now, defaultTime ?? new TimeOnly(9, 0));
}
