using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentReader
{
    public static IDocumentReader<D, HK> WithIO<D, HK>(
        IDbContext context)
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
        => new SqliteDocumentReaderWithIO<D, HK>(context);

    public static IDocumentReader<D, HK, RK> WithIO<D, HK, RK>(
        IDbContext context)
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
        => new SqliteDocumentReaderWithIO<D, HK, RK>(context);

    private sealed class SqliteDocumentReaderWithIO<D, HK>(
        IDbContext context)
        : IDocumentReader<D, HK>
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentReader<D, HK> reader = new(context);

        public Eff<Option<D>> Find(HK hashKey) =>
            Lift(token => reader.FindAsync(hashKey, token));
    }

    private sealed class SqliteDocumentReaderWithIO<D, HK, RK>(
        IDbContext context)
        : IDocumentReader<D, HK, RK>
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
    {
        private readonly SqliteDocumentReader<D, HK, RK> reader = new(context);

        public Eff<Iterable<D>> Find(HK hashKey, RangeKeyCondition range) =>
            Lift(token => reader.FindAsync(hashKey, range, token));

        public Eff<Option<D>> Find(HK hashKey, RK rangeKey) =>
            Lift(token => reader.FindAsync(hashKey, rangeKey, token));

        public Eff<Iterable<D>> Where(HK hashKey, Func<D, bool> predicate) =>
            Lift(token => reader.WhereAsync(hashKey, predicate, token));
    }

    private static Eff<T> Lift<T>(Func<CancellationToken, Task<T>> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}
