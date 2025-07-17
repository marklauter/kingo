namespace Kingo.Storage.Sqlite;

public sealed record SqliteConnectionFactoryOptions(
    string ConnectionString,
    bool EnableWAL);
