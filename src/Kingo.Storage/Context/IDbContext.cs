using Kingo.Storage.Sqlite;
using System.Data.Common;

namespace Kingo.Storage.Context;

public interface IDbContext
{
    Task ExecuteAsync(Func<DbConnection, DbTransaction, Task> operation, CancellationToken token);
    Task<T> ExecuteAsync<T>(Func<DbConnection, DbTransaction, Task<T>> operation, CancellationToken token);
    Task ApplyMigrationsAsync(Migrations migrations, CancellationToken token);
}
