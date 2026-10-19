using System.Text.RegularExpressions;

namespace Memento.Core.Tasks.Parsing;

internal static partial class TagExtractor
{
    [GeneratedRegex(@"#(\w+)")]
    private static partial Regex TagRegex();

    /// <summary>Cuts every #tag out; returns them lower-cased, without duplicates, in order of appearance.</summary>
    public static IReadOnlyList<string> Extract(ParseBuffer buffer)
    {
        var tags = TagRegex().Matches(buffer.Text)
            .Select(match => match.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
        buffer.RemoveAll(TagRegex());
        return tags;
    }
}
