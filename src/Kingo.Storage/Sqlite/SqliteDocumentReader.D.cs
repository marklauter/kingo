using Dapper;
using Kingo.Storage.Context;
using LanguageExt;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentReader<D>(IDbContext context)
{
    private static readonly string TablePrefix = DocumentTypeCache<D>.Name;
    private static readonly string HashKeyName = DocumentTypeCache<D>.HashKeyProperty.Name;
    private static readonly Option<PropertyInfo> RangeKeyProperty = DocumentTypeCache<D>.RangeKeyProperty;
    private static readonly Option<PropertyInfo> VersionProperty = DocumentTypeCache<D>.VersionProperty;

    public async Task<Option<D>> FindAsync<HK>(
        HK hashKey,
        Option<object> rangeKey,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK> =>
        RangeKeyProperty.IsSome && rangeKey.IsNone
            ? throw new InvalidOperationException($"Document {typeof(D).Name} defines a range key, but no range key predicate was provided.")
            : (await QueryAsync(
                new Query<D, HK>(hashKey, rangeKey.Map(RangeKeyCondition.IsEqualTo)),
                token)).Head;

    public async Task<Seq<D>> QueryAsync<HK>(
        Query<D, HK> query,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK> =>
        Prelude.Seq(FilterDocuments(
            query,
            await ReadDocumentsAsync(BuildQuery(query), token)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<IEnumerable<D>> ReadDocumentsAsync(
        (string Sql, object Parameters) queryContext,
        CancellationToken token) =>
        context.ExecuteAsync((db, tx) =>
            db.QueryAsync<D>(queryContext.Sql, queryContext.Parameters, tx),
            token);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<D> FilterDocuments<HK>(Query<D, HK> query, IEnumerable<D> documents)
        where HK : IEquatable<HK>, IComparable<HK> =>
        query.Filter.Match(
            Some: documents.Where,
            None: () => documents);

    private static (string Sql, Dictionary<string, object> Parameters) BuildQuery<HK>(Query<D, HK> query)
        where HK : IEquatable<HK>, IComparable<HK> =>
        AppendRangeKeyClause(
            AppendWhereClause(
                AppendJoinClause(
                    AppendSelectClause(
                        new StringBuilder()))),
            query,
            new Dictionary<string, object>
            {
                ["HashKey"] = query.HashKey
            });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendSelectClause(StringBuilder builder) =>
        builder.AppendLine(CultureInfo.InvariantCulture, $"select b.* from {TablePrefix}_header a");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendJoinClause(StringBuilder builder)
    {
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"join {TablePrefix}_journal b on b.{HashKeyName} = a.{HashKeyName}");
        return VersionProperty.Match(
            Some: pi =>
                builder.AppendLine(CultureInfo.InvariantCulture, $"and b.{pi.Name} = a.{pi.Name}"),
            None: () => builder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendWhereClause(StringBuilder builder) =>
        builder.AppendLine(CultureInfo.InvariantCulture, $"where a.{HashKeyName} = @HashKey");

    private static (string Sql, Dictionary<string, object> Parameters) AppendRangeKeyClause<HK>(
        StringBuilder builder,
        Query<D, HK> query,
        Dictionary<string, object> parameters)
    where HK : IEquatable<HK>, IComparable<HK> =>
        (query.RangeKeyCondition
            .Match(
                Some: condition =>
                {
                    var pi = RangeKeyProperty.IfNone(() => throw new InvalidOperationException("document does not define a range key"));
                    return builder.AppendLine(BuildRangeKeyClause(pi.Name, condition, parameters));
                },
                None: builder)
            .ToString(),
            parameters);

    private static string BuildRangeKeyClause(
        string name,
        RangeKeyCondition condition,
        Dictionary<string, object> parameters)
    {
        var (op, values) = ToOpNValues(condition);
        AppendParameters(parameters, values);
        return op switch
        {
            "BET" => $"and a.{name} between @RangeKeyStart and @RangeKeyEnd",
            "GTLT" => $"and @RangeKeyStart < a.{name} and a.{name} < @RangeKeyEnd",
            _ => $"and a.{name} {op} @RangeKey"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendParameters(
        Dictionary<string, object> parameters,
        object[] values)
    {
        if (values.Length == 1)
        {
            parameters.Add("RangeKey", values[0]);
        }
        else
        {
            parameters.Add("RangeKeyStart", values[0]);
            parameters.Add("RangeKeyEnd", values[1]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (string op, object[] values) ToOpNValues(
        RangeKeyCondition condition) =>
        condition switch
        {
            EqualsCondition c => ("=", [c.Key]),
            GreaterThanCondition c => (">", [c.Key]),
            GreaterThanOrEqualCondition c => (">=", [c.Key]),
            LessThanCondition c => ("<", [c.Key]),
            LessThanOrEqualCondition c => ("<=", [c.Key]),
            BetweenInclusiveCondition c => ("BET", [c.LowerBound, c.UpperBound]),
            BetweenExlusiveCondition c => ("GTLT", [c.LowerBound, c.UpperBound]),
            _ => throw new NotSupportedException("Unsupported range key condition.")
        };
}
