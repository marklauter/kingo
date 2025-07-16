namespace Kingo.Storage.Sqlite;

public sealed record Migrations
{
    // kvp: (name, script)
    public Migrations(IEnumerable<KeyValuePair<string, string>> sqlScripts) =>
        SqlScripts = sqlScripts.Append(new KeyValuePair<string, string>(
            "migrations-table", """
            CREATE TABLE IF NOT EXISTS migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                date TEXT NOT NULL
            );
        
            CREATE INDEX IF NOT EXISTS idx_migrations_name ON migrations (name);
        """));

    public IEnumerable<KeyValuePair<string, string>> SqlScripts { get; }
}
