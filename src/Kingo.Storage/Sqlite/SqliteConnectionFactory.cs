using Kingo.Storage.Context;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Kingo.Storage.Sqlite;

public sealed class SqliteConnectionFactory(
    SqliteConnectionFactoryOptions options)
    : IDbConnectionFactory
{
    private readonly string connectionString = new SqliteConnectionStringBuilder(options.ConnectionString)
    {
        // todo: some of these these should be configurable
        Pooling = true,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true,
    }
    .ToString();

    private bool enableWAL = options.EnableWAL;

    public void ClearAllPools() => SqliteConnection.ClearAllPools();

    public async Task<DbConnection> OpenAsync(CancellationToken token)
    {
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(token);
        if (enableWAL)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = 'wal'";
            _ = await command.ExecuteNonQueryAsync(token);
            enableWAL = false;
        }

        return connection;
    }
}
