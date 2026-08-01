namespace Kingo;

public static class IdentifierGrammar
{
    public const string Name = "[A-Za-z_][A-Za-z0-9_]*";

    public const char TheorySeparator = '/';

    public const char ResourceSeparator = ':';

    public const char RelationSeparator = '#';

    public const char SubjectSeparator = '@';

    public const string NamePattern = $"^{Name}$";

    public const string NamespacePathPattern = "^" + Name + "/" + Name + "$";

    public const string IdPattern = @"^[^\s\p{C}]+$";
}
