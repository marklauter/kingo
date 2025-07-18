using LanguageExt;

namespace Kingo.Storage.Sqlite;

public sealed record Migrations
{
    private const string MigrationsTableKey = "migrations-table";
    private const string MigrationsTableDdl = """
            CREATE TABLE IF NOT EXISTS migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                date TEXT NOT NULL
            );
        
            CREATE INDEX IF NOT EXISTS idx_migrations_name ON migrations (name);
            """;

    public Migrations(Map<string, string> scripts) =>
        Scripts = scripts.ContainsKey(MigrationsTableKey)
        ? scripts
        : scripts.Add(MigrationsTableKey, MigrationsTableDdl);

    public static Migrations Cons() => new(Map<string, string>.Empty);
    public static Migrations Cons(Map<string, string> scripts) => new(scripts);

    public Migrations Add(string name, string script) => Cons(Scripts.Add(name, script));

    public Map<string, string> Scripts { get; }
}
