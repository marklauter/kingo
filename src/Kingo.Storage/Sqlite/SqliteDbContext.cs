using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Kingo.Storage.Sqlite;

public sealed class SqliteDbContext(ConnectionFactory factory)
    : IDbContext
{
    public async Task ExecuteAsync(Func<SqliteConnection, IDbTransaction, Task> operation, CancellationToken token)
    {
        using var connection = await factory.OpenAsync(token);
        await connection.ExecuteAsync(operation, token);
    }

    public async Task<T> ExecuteAsync<T>(Func<SqliteConnection, IDbTransaction, Task<T>> operation, CancellationToken token)
    {
        using var connection = await factory.OpenAsync(token);
        return await connection.ExecuteAsync(operation, token);
    }

    public async Task PerformMigrationsAsync(Migrations migrations, CancellationToken token)
    {
        // todo: check migrations table (migration name, date)
        // read all migrations from DB, then subtract them from the incoming migration set
        // todo: write completed migrations to migrations table (name, date)
        using var connection = await factory.OpenAsync(token);
        foreach (var migration in migrations.SqlScripts)
        {
            await connection.ExecuteAsync(async (c, t) => await MigrationOperationAsync(c, t, migration.Key, migration.Value),
            token);
        }
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

static file class ConnectionExtensions
{
    public static async Task ExecuteAsync(this SqliteConnection connection, Func<SqliteConnection, IDbTransaction, Task> operation, CancellationToken token)
    {
        await using var transaction = await connection.BeginTransactionAsync(token);
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

    public static async Task<T> ExecuteAsync<T>(this SqliteConnection connection, Func<SqliteConnection, IDbTransaction, Task<T>> operation, CancellationToken token)
    {
        await using var transaction = await connection.BeginTransactionAsync(token);
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
