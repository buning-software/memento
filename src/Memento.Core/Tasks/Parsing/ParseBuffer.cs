using System.Text.RegularExpressions;

namespace Memento.Core.Tasks.Parsing;

/// <summary>The input line while it is parsed: every recognised part is cut out; what remains is the title.</summary>
internal sealed partial class ParseBuffer(string text)
{
    public string Text { get; private set; } = text;

    /// <summary>Replaces the match with a space, so the words around it stay apart.</summary>
    public void Cut(Match match) => Text = Text.Remove(match.Index, match.Length).Insert(match.Index, " ");

    public void RemoveAll(Regex regex) => Text = regex.Replace(Text, " ");

    /// <summary>Turns every run of whitespace into a single space.</summary>
    public static string CollapseWhitespace(string text) => WhitespaceRegex().Replace(text, " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
