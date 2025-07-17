using Kingo.Storage.Sqlite;
using System.Data.Common;

namespace Kingo.Storage.Db;

public interface IDbContext
{
    Task ExecuteAsync(Func<DbConnection, DbTransaction, Task> operation, CancellationToken token);
    Task<T> ExecuteAsync<T>(Func<DbConnection, DbTransaction, Task<T>> operation, CancellationToken token);
    Task PerformMigrationsAsync(Migrations migrations, CancellationToken token);
}
