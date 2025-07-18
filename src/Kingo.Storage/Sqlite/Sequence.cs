using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using System.Data.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public sealed class Sequence<N>(
    IDbContext context,
    Identifier table,
    Key name)
    where N : INumber<N>
{
    private readonly record struct ReadParam(Key Key);
    private readonly ReadParam readParam = new(name);

    public async Task<N> NextAsync(CancellationToken cancellationToken)
    {
        var backoff = new Backoff(10);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await context.ExecuteAsync(NextAsync, cancellationToken) is (true, var n))
                return n;

            await backoff.WaitAsync(cancellationToken);
        }
    }

    private async Task<(bool success, N n)> NextAsync(DbConnection db, DbTransaction tx) =>
        await WriteAsync(await ReadAsync(db, tx), db, tx);

    private readonly string read = $"select value from {table} where key = @Key";
    private async Task<(bool exists, N currentValue, N newValue)> ReadAsync(DbConnection db, DbTransaction tx) =>
        MapN((await db.QuerySingleOrDefaultAsync<N>(read, readParam, tx))!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (bool exists, N currentValue, N newValue) MapN(N n) =>
        N.IsZero(n)
            ? (false, N.Zero, N.One)
            : (true, n, n + N.One);

    private async Task<(bool success, N n)> WriteAsync(
        (bool exists, N currentValue, N newValue) n,
        DbConnection db,
        DbTransaction tx) =>
        await InsertOrUpdate(n, db, tx) == 1
            ? (true, n.newValue)
            : (false, n.currentValue);

    private readonly record struct InsertParam(Key Key, N Value);
    private readonly record struct UpdateParam(Key Key, N NewValue, N CurrentValue);

    private readonly string insert = $"insert into {table} (key, value) values (@Key, @Value)";
    private readonly string update = $"update {table} set value = @NewValue where key = @Key and value = @CurrentValue";
    private Task<int> InsertOrUpdate(
        (bool exists, N currentValue, N newValue) n,
        DbConnection db,
        DbTransaction tx) =>
        n.exists
            ? db.ExecuteAsync(update, new UpdateParam(readParam.Key, n.newValue, n.currentValue), tx)
            : db.ExecuteAsync(insert, new InsertParam(readParam.Key, n.newValue), tx);
}
