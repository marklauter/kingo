namespace Kingo;

/// <summary>
/// The character rules and delimiters the identifier grammar is built from ([[identifiers]]).
/// <para>
/// <see cref="Name"/> is the <c>⟨name⟩</c> production, the character rule behind all four name positions a document has: the <c>domain:</c> value, the namespace
/// keys, the relationship names, and the names written inside a rewrite. <see cref="DomainName"/>, <see cref="NamespaceName"/>, and <see cref="RelationshipName"/>
/// are exactly <see cref="NamePattern"/>. They share one const deliberately so the three rules stay identical. The reservation of <c>this</c> as a relationship
/// name is the rewrite grammar's, enforced by the SDL parser ([[specs]]), not a character rule here. The one qualified path, <see cref="NamespacePath"/>, is two
/// names joined by <see cref="DomainSeparator"/>. The
/// rules live here rather than on one of the identifier types because none of them owns the production. Each type composes its anchored pattern from these
/// constants, so the grammars cannot drift apart.
/// </para>
/// </summary>
public static class IdentifierGrammar
{
    /// <summary>One name: a letter or underscore, then letters, digits, and underscores.</summary>
    public const string Name = "[A-Za-z_][A-Za-z0-9_]*";

    /// <summary>The delimiter between a domain and a namespace.</summary>
    public const char DomainSeparator = '/';

    /// <summary>The delimiter between a namespace and a resource id.</summary>
    public const char ResourceSeparator = ':';

    /// <summary>The marker that introduces a relation.</summary>
    public const char RelationSeparator = '#';

    /// <summary>The delimiter between a subject-set and a subject.</summary>
    public const char SubjectSeparator = '@';

    /// <summary>A bare name, for example <c>io</c>, <c>file</c>, or <c>viewer</c>.</summary>
    public const string NamePattern = $"^{Name}$";

    /// <summary>A namespace path, for example <c>io/file</c>. The <c>/</c> is <see cref="DomainSeparator"/> as a regex literal. The pattern must stay a compile-time constant for the source generator, and a <c>char</c> does not fold into one.</summary>
    public const string NamespacePathPattern = "^" + Name + "/" + Name + "$";

    // ---------------------------------------------------------------------------------------------------------------
    // The caller's grammar, not Kingo's.
    //
    // Everything above this line is a rule Kingo owns and interprets. Everything below is a rule about data the caller
    // owns: a resource id and a subject id are the caller's to shape, and Kingo only compares them
    // ([[split-identities-at-ownership-boundaries]]). The rule below is not the name rule, is not composed from
    // <see cref="Name"/>, and must never be tidied into it.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// An id the caller owns, either a resource id or a subject id. Kingo does not interpret it, so the rule admits the real shapes callers bring: GUIDs,
    /// integers, URNs, URIs, emails, and UPNs. It requires only a non-empty run of visible characters with no whitespace and no control characters. It is
    /// deliberately not composed from <see cref="Name"/>: an id is the caller's to shape, not Kingo's ([[split-identities-at-ownership-boundaries]]).
    /// </summary>
    public const string IdPattern = @"^[^\s\p{C}]+$";
}
