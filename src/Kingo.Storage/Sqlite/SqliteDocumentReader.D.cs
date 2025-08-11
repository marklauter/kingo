using Dapper;
using Kingo.Storage.Context;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentReader<D>(IDbContext context)
    : IDocumentReader<D>
{
    private static readonly string TablePrefix = DocumentTypeCache<D>.Name;
    private static readonly string HashKeyName = DocumentTypeCache<D>.HashKeyProperty.Name;
    private static readonly PropertyInfo? RangeKeyProperty = DocumentTypeCache<D>.RangeKeyProperty;
    private static readonly PropertyInfo? VersionProperty = DocumentTypeCache<D>.VersionProperty;

    public async Task<D?> FindAsync<HK>(
        HK hashKey,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK> =>
        (await QueryAsync(
            new Query<D, HK>(hashKey),
            token)).FirstOrDefault();

    public async Task<D?> FindAsync<HK, RK>(
        HK hashKey,
        RK rangeKey,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        (await QueryAsync(
            new Query<D, HK>(hashKey, RangeKeyCondition.IsEqualTo(rangeKey)),
            token)).FirstOrDefault();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<IEnumerable<D>> QueryAsync<HK>(
        Query<D, HK> query,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK> =>
        FilterDocuments(
            query,
            await ReadDocumentsAsync(SqlBuilder<D>.Build(query), token));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<IEnumerable<D>> QueryAsync<HK, N>(
        Query<D, HK, N> query,
        CancellationToken token)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N> =>
        FilterDocuments(
            query,
            await ReadDocumentsAsync(SqlBuilder<D>.Build(query), token));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<IEnumerable<D>> ReadDocumentsAsync(
        (string Sql, object Parameters) queryContext,
        CancellationToken token) =>
        context.ExecuteAsync((db, tx) =>
            db.QueryAsync<D>(queryContext.Sql, queryContext.Parameters, tx),
            token);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<D> FilterDocuments<HK>(
        Query<D, HK> query,
        IEnumerable<D> documents)
        where HK : IEquatable<HK>, IComparable<HK> =>
        query.Filter is Func<D, bool> filter
            ? documents.Where(filter)
            : documents;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<D> FilterDocuments<HK, N>(
        Query<D, HK, N> query,
        IEnumerable<D> documents)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N> =>
        query.Filter is Func<D, bool> filter
            ? documents.Where(filter)
            : documents;
}

static file class SqlBuilder<D>
{
    private static readonly string TablePrefix = DocumentTypeCache<D>.Name;
    private static readonly string HashKeyName = DocumentTypeCache<D>.HashKeyProperty.Name;
    private static readonly PropertyInfo? RangeKeyProperty = DocumentTypeCache<D>.RangeKeyProperty;
    private static readonly bool HasVersion = DocumentTypeCache<D>.VersionProperty is not null;
    private static readonly string SelectClause;
    private static readonly string WhereClause;
    private static readonly string JoinClause;
    private static readonly string BetweenClause;
    private static readonly string GTLTClause;
    private static readonly string OperatorClausePrefix;
    private static readonly string OperatorClauseSuffix;

    static SqlBuilder()
    {
        SelectClause = HasVersion
        ? $"select b.* from {TablePrefix}_header a"
        : $"select * from {TablePrefix}";

        WhereClause = HasVersion
        ? $"where a.{HashKeyName} = @HashKey"
        : $"where {HashKeyName} = @HashKey";

        JoinClause = HasVersion
            ? $"join {TablePrefix}_journal b on b.{HashKeyName} = a.{HashKeyName} and b.{DocumentTypeCache<D>.VersionProperty!.Name} = a.{DocumentTypeCache<D>.VersionProperty!.Name}"
            : string.Empty;

        var rangeKeyName = DocumentTypeCache<D>.RangeKeyProperty is PropertyInfo pi
            ? pi.Name
            : string.Empty;

        BetweenClause = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and a.{rangeKeyName} between @RangeKeyStart and @RangeKeyEnd"
                : $"and {rangeKeyName} between @RangeKeyStart and @RangeKeyEnd"
            : string.Empty;
        GTLTClause = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and @RangeKeyStart < a.{rangeKeyName} and a.{rangeKeyName} < @RangeKeyEnd"
                : $"and @RangeKeyStart < {rangeKeyName} and {rangeKeyName} < @RangeKeyEnd"
            : string.Empty;
        OperatorClausePrefix = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and a.{rangeKeyName}"
                : $"and {rangeKeyName}"
            : string.Empty;
        OperatorClauseSuffix = rangeKeyName != string.Empty
            ? "@RangeKey"
            : string.Empty;
    }

    public static (string Sql, Dictionary<string, object> Parameters) Build<HK>(Query<D, HK> query)
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

    public static (string Sql, Dictionary<string, object> Parameters) Build<HK, N>(Query<D, HK, N> query)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N> =>
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

    private static (string Sql, Dictionary<string, object> Parameters) AppendRangeKeyClause<HK>(
        StringBuilder builder,
        Query<D, HK> query,
        Dictionary<string, object> parameters)
        where HK : IEquatable<HK>, IComparable<HK> =>
        ((query.RangeKeyCondition is RangeKeyCondition condition
            ? builder.AppendLine(BuildRangeKeyClause(condition, parameters))
            : builder)
            .ToString(),
            parameters);

    private static void ThrowIfRangeKeyPropertyIsNull()
    {
        if (RangeKeyProperty is null)
            throw new InvalidOperationException("document does not define a range key");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendWhereClause(StringBuilder builder) =>
        builder.AppendLine(WhereClause);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendJoinClause(StringBuilder builder) =>
        HasVersion
        ? builder.AppendLine(JoinClause)
        : builder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringBuilder AppendSelectClause(StringBuilder builder) =>
        builder.AppendLine(SelectClause);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildRangeKeyClause(
        RangeKeyCondition condition,
        Dictionary<string, object> parameters)
    {
        ThrowIfRangeKeyPropertyIsNull();
        var (clause, args) = ToOperatorClauseAndArgs(condition);
        AppendParameters(parameters, args);
        return clause;
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
    private static (string clause, object[] args) ToOperatorClauseAndArgs(
        RangeKeyCondition condition) =>
        condition switch
        {
            EqualsCondition c => (OperatorClause("="), [c.Key]),
            GreaterThanCondition c => (OperatorClause(">"), [c.Key]),
            GreaterThanOrEqualCondition c => (OperatorClause(">="), [c.Key]),
            LessThanCondition c => (OperatorClause("<"), [c.Key]),
            LessThanOrEqualCondition c => (OperatorClause("<="), [c.Key]),
            BetweenInclusiveCondition c => (BetweenClause, [c.LowerBound, c.UpperBound]),
            BetweenExlusiveCondition c => (GTLTClause, [c.LowerBound, c.UpperBound]),
            _ => throw new NotSupportedException("Unsupported range key condition.")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string OperatorClause(string op) =>
        $"{OperatorClausePrefix} {op} {OperatorClauseSuffix}";
}
