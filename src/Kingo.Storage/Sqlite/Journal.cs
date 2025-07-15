using Dapper;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class Journal<HK>(
    SqliteConnection connection,
    Key table)
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct InsertParam(HK HashKey, Revision Version, string Data)
    {
        public InsertParam(Document<HK> document) : this(document.HashKey, document.Version, document.Data.Serialize()) { }
    }

    private readonly string insert = $"insert into {table}_journal (hashkey, version, data) values (@HashKey, @Version, @Data);";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> InsertAsync(Document<HK> document, IDbTransaction transaction) =>
        connection.ExecuteAsync(insert, new InsertParam(document), transaction);
}
