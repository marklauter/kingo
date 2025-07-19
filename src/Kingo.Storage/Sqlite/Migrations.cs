using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage.Sqlite;

public sealed record Migrations(Map<string, string> Scripts)
{
    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "explicit empty is desired")]
    public static Migrations Empty() => new(Map<string, string>.Empty);
    public static Migrations Cons(Map<string, string> scripts) => new(scripts);

    public Migrations Add(string name, string script) => Cons(Scripts.Add(name, script));
}
