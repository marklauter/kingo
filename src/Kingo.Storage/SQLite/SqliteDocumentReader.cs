using Kingo.Storage.Context;
using LanguageExt;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentReader
{
    public static IDocumentReader<D> WithIO<D>(IDbContext context)
        => new SqliteDocumentReaderWithIO<D>(context);

    private sealed class SqliteDocumentReaderWithIO<D>(IDbContext context)
        : IDocumentReader<D>
    {
        private readonly SqliteDocumentReader<D> reader =
            new(context);

        public Eff<Option<D>> Find<HK>(HK hashKey, Option<object> rangeKey)
            where HK : IEquatable<HK>, IComparable<HK> =>
            Lift(token => reader.FindAsync(hashKey, rangeKey, token));

        public Eff<Seq<D>> Query<HK>(Query<D, HK> query)
            where HK : IEquatable<HK>, IComparable<HK> =>
            Lift(token => reader.QueryAsync(query, token));
    }

    private static Eff<T> Lift<T>(Func<CancellationToken, Task<T>> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}
