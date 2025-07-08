using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo;

public sealed record Subject(BigId Id, HashMap<Identifier, string> Claims)
{
    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer empty here")]
    public Subject(BigId id) : this(id, HashMap<Identifier, string>.Empty) { }
}

