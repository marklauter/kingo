using System.Data.Common;

namespace Kingo.Storage.Db;

public interface IConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken token);
}
