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
    private static readonly string VersionClause;
    private static readonly string SelectClause;
    private static readonly string WhereClause;
    private static readonly string JoinClause;
    private static readonly string RangeKeyBetweenClause;
    private static readonly string RangeKeyGTLTClause;
    private static readonly string RangeKeyOperatorClausePrefix;
    private static readonly string RangeKeyOperatorClauseSuffix;
    private const string HashKeyParamName = "HashKey";
    private const string VersionParamName = "Version";
    private const string RangeKeyParamName = "RangeKey";

    static SqlBuilder()
    {
        SelectClause = HasVersion
            ? $"select b.* from {TablePrefix}_header a"
            : $"select * from {TablePrefix}";

        WhereClause = HasVersion
            ? $"where a.{HashKeyName} = @{HashKeyParamName}"
            : $"where {HashKeyName} = @{HashKeyParamName}";

        var versionPropertyName = HasVersion
            ? DocumentTypeCache<D>.VersionProperty!.Name
            : string.Empty;

        // no version means no journal
        JoinClause = HasVersion
            ? $"join {TablePrefix}_journal b on b.{HashKeyName} = a.{HashKeyName} and b.{versionPropertyName} = a.{versionPropertyName}"
            : string.Empty;

        VersionClause = HasVersion
            ? $"and a.{versionPropertyName} = @{VersionParamName}"
            : string.Empty;

        var rangeKeyName = DocumentTypeCache<D>.RangeKeyProperty is PropertyInfo pi
            ? pi.Name
            : string.Empty;

        RangeKeyBetweenClause = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and a.{rangeKeyName} between @{RangeKeyParamName}Start and @{RangeKeyParamName}End"
                : $"and {rangeKeyName} between @{RangeKeyParamName}Start and @{RangeKeyParamName}End"
            : string.Empty;

        RangeKeyGTLTClause = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and @{RangeKeyParamName}Start < a.{rangeKeyName} and a.{rangeKeyName} < @{RangeKeyParamName}End"
                : $"and @{RangeKeyParamName}Start < {rangeKeyName} and {rangeKeyName} < @{RangeKeyParamName}End"
            : string.Empty;

        RangeKeyOperatorClausePrefix = rangeKeyName != string.Empty
            ? HasVersion
                ? $"and a.{rangeKeyName}"
                : $"and {rangeKeyName}"
            : string.Empty;

        RangeKeyOperatorClauseSuffix = rangeKeyName != string.Empty
            ? $"@{RangeKeyParamName}"
            : string.Empty;

    }

    public static (string Sql, Dictionary<string, object> Parameters) Build<HK>(Query<D, HK> query)
        where HK : IEquatable<HK>, IComparable<HK>
    {
        var sqlParams = new Dictionary<string, object>
        {
            [HashKeyParamName] = query.HashKey
        };

        return AppendRangeKeyClause(
            AppendWhereClause(
                AppendJoinClause(
                    AppendSelectClause(
                        new StringBuilder()))),
            query,
            sqlParams);
    }

    public static (string Sql, Dictionary<string, object> Parameters) Build<HK, N>(Query<D, HK, N> query)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N>
    {
        var sqlParams = new Dictionary<string, object>
        {
            [HashKeyParamName] = query.HashKey
        };

        return AppendRangeKeyClause(
            AppendVersionClause(
                AppendWhereClause(
                    AppendJoinClause(
                        AppendSelectClause(
                            new StringBuilder()))),
                query,
                sqlParams),
            query,
            sqlParams);
    }

    private static (string Sql, Dictionary<string, object> Parameters) AppendRangeKeyClause<HK>(
        StringBuilder builder,
        Query<D, HK> query,
        Dictionary<string, object> sqlParams)
        where HK : IEquatable<HK>, IComparable<HK> =>
        ((query.RangeKeyCondition is RangeKeyCondition condition
            ? builder.AppendLine(BuildRangeKeyClause(condition, sqlParams))
            : builder)
            .ToString(),
            sqlParams);

    private static StringBuilder AppendVersionClause<HK, N>(
        StringBuilder builder,
        Query<D, HK, N> query,
        Dictionary<string, object> sqlParams)
        where HK : IEquatable<HK>, IComparable<HK>
        where N : INumber<N>
    {
        ThrowIfMissingVersionProperty();
        if (HasVersion)
        {
            sqlParams.Add(VersionParamName, query.Version);
            return builder.AppendLine(VersionClause);
        }

        return builder;
    }

    private static void ThrowIfMissingVersionProperty()
    {
        if (!HasVersion)
            throw new KingoSqlBuilderException("document does not define a range key");
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
        Dictionary<string, object> sqlParams)
    {
        ThrowIfMissingRangeKeyProperty();
        var (clause, args) = ToOperatorClauseAndArgs(condition);
        AppendRangeKeyParameters(sqlParams, args);
        return clause;
    }
    private static void ThrowIfMissingRangeKeyProperty()
    {
        if (RangeKeyProperty is null)
            throw new KingoSqlBuilderException("document does not define a range key");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendRangeKeyParameters(
        Dictionary<string, object> sqlParams,
        object[] values)
    {
        if (values.Length == 1)
        {
            sqlParams.Add("RangeKey", values[0]);
        }
        else
        {
            sqlParams.Add("RangeKeyStart", values[0]);
            sqlParams.Add("RangeKeyEnd", values[1]);
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
            BetweenInclusiveCondition c => (RangeKeyBetweenClause, [c.LowerBound, c.UpperBound]),
            BetweenExlusiveCondition c => (RangeKeyGTLTClause, [c.LowerBound, c.UpperBound]),
            _ => throw new NotSupportedException($"unsupported range key condition {condition}")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string OperatorClause(string op) =>
        $"{RangeKeyOperatorClausePrefix} {op} {RangeKeyOperatorClauseSuffix}";
}
