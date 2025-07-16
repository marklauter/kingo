using Microsoft.Data.Sqlite;
using System.Data;

namespace Kingo.Storage.Sqlite;

public interface IDbContext
{
    Task ExecuteAsync(Func<SqliteConnection, IDbTransaction, Task> operation, CancellationToken token);
    Task<T> ExecuteAsync<T>(Func<SqliteConnection, IDbTransaction, Task<T>> operation, CancellationToken token);
    Task PerformMigrationsAsync(Migrations migrations, CancellationToken token);
}
