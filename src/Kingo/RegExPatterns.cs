using System.Text.RegularExpressions;

namespace Kingo;

internal partial class RegExPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Compiled |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]+$", PatternOptions)]
    public static partial Regex Identifier();
}
