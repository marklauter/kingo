using Dapper;
using Kingo.Storage.Db;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentReader<D, HK>(
    IDbContext context)
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkParam(HK HashKey);
    private static readonly string FindStatement =
        $"select b.* from {DocumentTypeCache<D>.TypeName}_header a join {DocumentTypeCache<D>.TypeName}_journal b on b.hashkey = a.hashkey and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync(HK hashKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(FindStatement, new HkParam(hashKey), tx),
            token);
}
