using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage.Sqlite;

public sealed record Migrations(ImmutableDictionary<string, string> Scripts)
{
    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "explicit empty is desired")]
    public static Migrations Empty() => new(ImmutableDictionary<string, string>.Empty);
    public static Migrations Cons(ImmutableDictionary<string, string> scripts) => new(scripts);
    public Migrations Add(string name, string script) => Cons(Scripts.Add(name, script));
}
