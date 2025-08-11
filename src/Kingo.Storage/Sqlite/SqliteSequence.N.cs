using Dapper;
using Kingo.Storage.Context;
using Kingo.Storage.Keys;
using System.Data.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteSequence<N>(
    IDbContext context,
    Identifier table)
    : ISequence<N>
    where N : INumber<N>
{
    private readonly record struct ReadParam(Key Key);

    public async Task<N> NextAsync(Key name, CancellationToken cancellationToken)
    {
        var backoff = new Backoff(10);
        var readParam = new ReadParam(name);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await context.ExecuteAsync((db, tx) =>
                NextAsync(readParam, db, tx), cancellationToken) is (true, var n))
                return n;

            await backoff.WaitAsync(cancellationToken);
        }
    }

    private async Task<(bool success, N n)> NextAsync(ReadParam readParam, DbConnection db, DbTransaction tx) =>
        await WriteAsync(await ReadAsync(readParam, db, tx), readParam, db, tx);

    private readonly string read = $"select value from {table} where key = @Key";
    private async Task<(bool exists, N currentValue, N newValue)> ReadAsync(ReadParam readParam, DbConnection db, DbTransaction tx) =>
        MapN((await db.QuerySingleOrDefaultAsync<N>(read, readParam, tx))!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (bool exists, N currentValue, N newValue) MapN(N n) =>
        N.IsZero(n)
            ? (false, N.Zero, N.One)
            : (true, n, n + N.One);

    private async Task<(bool success, N n)> WriteAsync(
        (bool exists, N currentValue, N newValue) n,
        ReadParam readParam,
        DbConnection db,
        DbTransaction tx) =>
        await InsertOrUpdate(n, readParam, db, tx) == 1
            ? (true, n.newValue)
            : (false, n.currentValue);

    private readonly record struct InsertParam(Key Key, N Value);
    private readonly record struct UpdateParam(Key Key, N NewValue, N CurrentValue);

    private readonly string insert = $"insert into {table} (key, value) values (@Key, @Value)";
    private readonly string update = $"update {table} set value = @NewValue where key = @Key and value = @CurrentValue";
    private Task<int> InsertOrUpdate(
        (bool exists, N currentValue, N newValue) n,
        ReadParam readParam,
        DbConnection db,
        DbTransaction tx) =>
        n.exists
            ? db.ExecuteAsync(update, new UpdateParam(readParam.Key, n.newValue, n.currentValue), tx)
            : db.ExecuteAsync(insert, new InsertParam(readParam.Key, n.newValue), tx);
}
