using Dapper;
using Kingo.Storage.Context;
using Kingo.Storage.Sqlite;
using System.Data;
using System.Data.Common;

namespace Kingo.Storage.Context;

public sealed class DbContext(IDbConnectionFactory factory)
    : IDbContext
{
    public void ClearAllPools() => factory.ClearAllPools();

    public async Task ExecuteAsync(Func<DbConnection, DbTransaction, Task> operation, CancellationToken token)
    {
        using var connection = await factory.OpenAsync(token);
        await connection.ExecuteAsync(operation, token);
    }

    public async Task<T> ExecuteAsync<T>(Func<DbConnection, DbTransaction, Task<T>> operation, CancellationToken token)
    {
        using var connection = await factory.OpenAsync(token);
        return await connection.ExecuteAsync(operation, token);
    }

    private const string MigrationsTableDdl = """
            CREATE TABLE IF NOT EXISTS migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                date TEXT NOT NULL
            );
        
            CREATE INDEX IF NOT EXISTS idx_migrations_name ON migrations (name);
            """;

    public async Task ApplyMigrationsAsync(Migrations migrations, CancellationToken token)
    {
        using var connection = await factory.OpenAsync(token);
        _ = await connection.ExecuteAsync((db, tx) =>
                db.ExecuteAsync(MigrationsTableDdl, null, tx),
                token);

        await connection.ExecuteAsync(async (db, tx) =>
        {
            foreach (var (Key, Value) in migrations.Scripts)
                await MigrationOperationAsync(db, tx, Key, Value);
        },
        token);
    }

    private static readonly string InsertMigration = "insert into migrations (name, date) values (@Name, @Date)";
    private static readonly string MigrationExists = "select exists(select 1 from migrations where name = @Name)";
    private readonly record struct MigrationParam(string Name, string Date);
    private readonly record struct MigrationExistsParam(string Name);
    private static async Task MigrationOperationAsync(
        IDbConnection db,
        IDbTransaction tx,
        string name,
        string script)
    {
        if (await db.QuerySingleAsync<bool>(MigrationExists, new MigrationExistsParam(name), tx))
            return;

        _ = await db.ExecuteAsync(script, null, tx);
        _ = await db.ExecuteAsync(InsertMigration, new MigrationParam(name, DateTime.UtcNow.ToString("o")), tx);
    }
}

static file class TxMan
{
    public static async Task ExecuteAsync(
        this DbConnection connection,
        Func<DbConnection, DbTransaction, Task> operation,
        CancellationToken token)
    {
        await using var transaction = await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, token);

        try
        {
            await operation(connection, transaction);
            await transaction.CommitAsync(token);
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    public static async Task<T> ExecuteAsync<T>(
        this DbConnection connection,
        Func<DbConnection, DbTransaction, Task<T>> operation,
        CancellationToken token)
    {
        await using var transaction = await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, token);

        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync(token);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
}
