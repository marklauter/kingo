using LanguageExt;

namespace Kingo.Storage.Sqlite;

public sealed record Migrations(Map<string, string> Scripts)
{
    public static Migrations Cons() => new(Map<string, string>.Empty);
    public static Migrations Cons(Map<string, string> scripts) => new(scripts);

    public Migrations Add(string name, string script) => Cons(Scripts.Add(name, script));
}
