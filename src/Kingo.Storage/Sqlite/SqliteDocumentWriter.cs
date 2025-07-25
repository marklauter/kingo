using Kingo.Storage.Context;
using LanguageExt;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentWriter
{
    public static IDocumentWriter<D, HK> WithIO<D, HK>(
        IDbContext context)
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK> =>
        new SqliteDocumentWriterWithIO<D, HK>(context);

    public static IDocumentWriter<D, HK, RK> WithIO<D, HK, RK>(
        IDbContext context)
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new SqliteDocumentWriterWithIO<D, HK, RK>(context);

    private sealed class SqliteDocumentWriterWithIO<D, HK>(
        IDbContext context)
        : IDocumentWriter<D, HK>
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentWriter<D, HK> writer = new(context);

        public Eff<Unit> Insert(D document) =>
            Lift(token => writer.InsertAsync(document, token));

        public Eff<Unit> InsertOrUpdate(D document) =>
            Lift(token => writer.InsertOrUpdateAsync(document, token));

        public Eff<Unit> Update(D document) =>
            Lift(token => writer.UpdateAsync(document, token));
    }

    private sealed class SqliteDocumentWriterWithIO<D, HK, RK>(
        IDbContext context)
        : IDocumentWriter<D, HK, RK>
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
    {
        private readonly SqliteDocumentWriter<D, HK, RK> writer = new(context);

        public Eff<Unit> Insert(D document) =>
            Lift(token => writer.InsertAsync(document, token));

        public Eff<Unit> InsertOrUpdate(D document) =>
            Lift(token => writer.InsertOrUpdateAsync(document, token));

        public Eff<Unit> Update(D document) =>
            Lift(token => writer.UpdateAsync(document, token));
    }

    private static Eff<Unit> Lift(Func<CancellationToken, Task> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}
