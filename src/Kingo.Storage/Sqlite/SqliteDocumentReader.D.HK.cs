using Dapper;
using Kingo.Storage.Db;
using LanguageExt;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentReader<D>(
    IDbContext context)
{
    private readonly record struct HkParam<HK>(HK HashKey);
    private static readonly string FindStatement;

    static SqliteDocumentReader()
    {
        var tablePrefix = DocumentTypeCache<D>.Name;
        var hashKey = DocumentTypeCache<D>.HashKeyProperty.Name;
        var builder = new StringBuilder($"select b.* from {tablePrefix}_header a")
            .AppendLine(CultureInfo.InvariantCulture, $"join {tablePrefix}_journal b")
            .AppendLine(CultureInfo.InvariantCulture, $"on b.{hashKey} = a.{hashKey}");
        FindStatement = DocumentTypeCache<D>.VersionProperty
            .Match(
                Some: pi =>
                    builder.AppendLine(CultureInfo.InvariantCulture, $"and b.{pi.Name} = a.{pi.Name}"),
                None: () => builder
            )
            .AppendLine(CultureInfo.InvariantCulture, $"where a.{hashKey} = @HashKey")
            .ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync<HK>(HK hashKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(FindStatement, new HkParam<HK>(hashKey), tx),
            token);
}

internal sealed class SqliteDocumentReader<D, HK>(
    IDbContext context)
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkParam(HK HashKey);
    private static readonly string FindStatement =
        $"select b.* from {DocumentTypeCache<D>.Name}_header a join {DocumentTypeCache<D>.Name}_journal b on b.hashkey = a.hashkey and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync(HK hashKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(FindStatement, new HkParam(hashKey), tx),
            token);
}
