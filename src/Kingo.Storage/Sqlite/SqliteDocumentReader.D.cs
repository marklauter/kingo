using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

public record Query<D, HK>(HK HashKey, Option<RangeKeyCondition> RangeKeyCondition, Option<Func<D, bool>> Filter);

internal sealed class SqliteDocumentReader<D>(IDbContext context)
{
    private readonly record struct HkParam<HK>(HK HashKey);
    private readonly record struct HkRkParam<HK, RK>(HK HashKey, RK RangeKey);
    private static readonly string FindStatement;

    // todo: these can't be static - must be dynamic and based on the query
    static SqliteDocumentReader()
    {
        var tablePrefix = DocumentTypeCache<D>.Name;
        var hashKey = DocumentTypeCache<D>.HashKeyProperty.Name;
        var rangeKey = DocumentTypeCache<D>.RangeKeyProperty;
        var version = DocumentTypeCache<D>.VersionProperty;
        var builder = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"select b.* from {tablePrefix}_header a")
            .AppendLine(CultureInfo.InvariantCulture, $"join {tablePrefix}_journal b")
            .AppendLine(CultureInfo.InvariantCulture, $"on b.{hashKey} = a.{hashKey}");
        _ = version
            .Match(
                Some: pi =>
                    builder.AppendLine(CultureInfo.InvariantCulture, $"and b.{pi.Name} = a.{pi.Name}"),
                None: () => builder)
            .AppendLine(CultureInfo.InvariantCulture, $"where a.{hashKey} = @HashKey");
        FindStatement = rangeKey
            .Match(
                Some: pi =>
                    builder.AppendLine(CultureInfo.InvariantCulture, $"and a.{pi.Name} = @RangeKey"),
                None: () => builder)
            .ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync<HK>(Query<D, HK> query, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(FindStatement, new HkParam<HK>(query.HashKey), tx),
            token);
}
