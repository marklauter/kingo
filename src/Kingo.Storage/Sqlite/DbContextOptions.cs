namespace Kingo.Storage.Sqlite;

public sealed record DbContextOptions(
    string ConnectionString,
    bool EnableWAL);
