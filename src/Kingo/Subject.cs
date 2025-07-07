using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo;

public sealed record Subject(ShortId Id, HashMap<Identifier, string> Claims)
{
    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer empty here")]
    public Subject(ShortId id) : this(id, HashMap<Identifier, string>.Empty) { }
}

