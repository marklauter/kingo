using Microsoft.Data.Sqlite;
using System.Data;

namespace Kingo.Storage.Sqlite;

internal sealed class TransactionManager(SqliteConnection connection)
{
    public async Task ExecuteAsync(Func<IDbTransaction, Task> operation, CancellationToken token)
    {
        await using var transaction = await connection.BeginTransactionAsync(token);
        try
        {
            await operation(transaction);
            await transaction.CommitAsync(token);
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    public async Task<T> ExecuteAsync<T>(Func<IDbTransaction, Task<T>> operation, CancellationToken token)
    {
        await using var transaction = await connection.BeginTransactionAsync(token);
        try
        {
            var result = await operation(transaction);
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
