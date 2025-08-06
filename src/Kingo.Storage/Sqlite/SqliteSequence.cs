using Kingo.Storage.Context;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Numerics;

namespace Kingo.Storage.Sqlite;

public static class SqliteSequence
{
    public static ISequence<N> WithIO<N>(
        IDbContext context,
        Identifier table)
        where N : INumber<N>
        => new SqliteSequenceWithIO<N>(context, table);

    private sealed class SqliteSequenceWithIO<N>(
        IDbContext context,
        Identifier table)
        : ISequence<N> where N : INumber<N>
    {
        private readonly SqliteSequence<N> sequence =
            new(context, table);

        public Eff<N> NextAsync(Key name) =>
            Lift(token => sequence.NextAsync(name, token));

    }

    private static Eff<T> Lift<T>(Func<CancellationToken, Task<T>> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}
