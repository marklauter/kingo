using System.Data.Common;

namespace Kingo.Storage.Db;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken token);
}
