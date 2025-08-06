using Kingo.Storage.Context;
using LanguageExt;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentWriter
{
    public static IDocumentWriter<D> WithIO<D>(
        IDbContext context)
        => new SqliteDocumentWriterWithIO<D>(context);

    private sealed class SqliteDocumentWriterWithIO<D>(
        IDbContext context)
        : IDocumentWriter<D>
    {
        private readonly SqliteDocumentWriter<D> writer = new(context);

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
