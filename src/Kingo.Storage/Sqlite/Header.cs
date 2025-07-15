using Dapper;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class Header<HK>(
    SqliteConnection connection,
    Key table)
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkVersionParam(HK HashKey, Revision Version)
    {
        public HkVersionParam(Document<HK> document) : this(document.HashKey, document.Version) { }
    }

    private readonly record struct HashKeyParam(HK HashKey);

    private readonly string insert = $"insert into {table}_header (hashkey, version) values (@HashKey, @Version);";
    private readonly string update = $"update {table}_header set version = @Version where hashkey = @HashKey;";
    private readonly string exists = $"select exists(select 1 from {table}_header where hashkey = @HashKey);";
    private readonly string revision = $"select version from {table}_header where hashkey = @HashKey;";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> InsertAsync(Document<HK> document, IDbTransaction transaction) =>
        connection.ExecuteAsync(insert, new HkVersionParam(document), transaction);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> UpdateAsync(Document<HK> document, IDbTransaction transaction) =>
        connection.ExecuteAsync(update, new HkVersionParam(document), transaction);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> ExistsAsync(Document<HK> document, IDbTransaction transaction) =>
        await connection.QuerySingleAsync<bool>(exists, new HashKeyParam(document.HashKey), transaction);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Revision?> ReadRevisionAsync(Document<HK> document, IDbTransaction transaction) =>
        await connection.QuerySingleOrDefaultAsync<Revision?>(revision, new HashKeyParam(document.HashKey), transaction);
}
