using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Sqlite;
using System.Data;
using System.Data.Common;

namespace Kingo.Storage.Db;

public sealed class DbContext(IDbConnectionFactory factory)
    : IDbContext
{
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

    public async Task ApplyMigrationsAsync(Migrations migrations, CancellationToken token)
    {
        // todo: check migrations table (migration name, date)
        // read all migrations from DB, then subtract them from the incoming migration set
        // todo: write completed migrations to migrations table (name, date)
        using var connection = await factory.OpenAsync(token);
        foreach (var (Key, Value) in migrations.Scripts)
            await connection.ExecuteAsync(async (c, t) => await MigrationOperationAsync(c, t, Key, Value),
            token);
    }

    private static readonly string InsertMigration = "insert into migrations (name, date) values (@Name, @Date)";
    private static readonly string MigrationExists = "select exists(select 1 from migrations where name = @Name)";
    private readonly record struct MigrationParam(string Name, string Date);
    private readonly record struct MigrationExistsParam(string Name);
    private static async Task MigrationOperationAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        string name,
        string script)
    {
        if (await connection.QuerySingleAsync<bool>(MigrationExists, new MigrationExistsParam(name), transaction))
            return;

        _ = await connection.ExecuteAsync(script, null, transaction);
        _ = await connection.ExecuteAsync(InsertMigration, new MigrationParam(name, DateTime.UtcNow.ToString("o")), transaction);
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
