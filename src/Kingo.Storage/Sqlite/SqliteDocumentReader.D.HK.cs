using Dapper;
using Kingo.Storage.Db;
using LanguageExt;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

// todo: add rangekey support and the extra methods from the HK.RK class. this will eliminate the need for HK.RK class
internal sealed class SqliteDocumentReader<D>(IDbContext context)
{
    private readonly record struct HkParam<HK>(HK HashKey);
    private static readonly string FindStatement;

    static SqliteDocumentReader()
    {
        var tablePrefix = DocumentTypeCache<D>.Name;
        var hashKey = DocumentTypeCache<D>.HashKeyProperty.Name;
        var builder = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"select b.* from {tablePrefix}_header a")
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
