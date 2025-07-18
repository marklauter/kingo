using System.Data.Common;

namespace Kingo.Storage.Db;

public interface IDbConnectionFactory
{
    void ClearAllPools();
    Task<DbConnection> OpenAsync(CancellationToken token);
}
