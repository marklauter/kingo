using System.Data.Common;

namespace Kingo.Storage.Context;

public interface IDbConnectionFactory
{
    void ClearAllPools();
    Task<DbConnection> OpenAsync(CancellationToken token);
}
