using System.Text.RegularExpressions;

namespace Kingo.Policies;

internal sealed partial class RegExPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Compiled |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex NamespaceIdentifier();

    // allows the ... literal used by RelationIdentifier.Nothing
    [GeneratedRegex(@"^\.\.\.$|^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex RelationIdentifier();
}
