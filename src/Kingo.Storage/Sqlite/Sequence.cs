using Dapper;
using Kingo.Storage.Keys;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public sealed class Sequence<N>(
    SqliteConnection connection,
    Key name)
    where N : INumber<N>
{
    private readonly TransactionManager txman = new(connection);
    private readonly record struct ReadParam(Key HashKey);
    private readonly ReadParam readParam = new(Key.From($"seq/{name}"));

    public Task<N> NextAsync(CancellationToken cancellationToken) =>
        txman.ExecuteAsync(transaction => NextAsync(transaction, cancellationToken), cancellationToken);

    private async Task<N> NextAsync(IDbTransaction transaction, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await WriteAsync(await ReadAsync(transaction), transaction) is (true, var n))
                return n;
        }
    }

    private readonly string read = $"select value from seq where hashkey = @HashKey";
    private async Task<(bool exists, N currentValue, N newValue)> ReadAsync(IDbTransaction transaction) =>
        MapN((await connection.QuerySingleOrDefaultAsync<N>(read, readParam, transaction))!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (bool exists, N currentValue, N newValue) MapN(N n) =>
        N.IsZero(n) ? (false, N.Zero, N.One) : (true, n, n + N.One);

    private async Task<(bool success, N n)> WriteAsync((bool exists, N currentValue, N newValue) n, IDbTransaction transaction) =>
        await InsertOrUpdate(n, transaction) == 1
            ? (true, n.newValue)
            : (false, n.currentValue);

    private readonly record struct InsertParam(Key HashKey, N Value);
    private readonly record struct UpdateParam(Key HashKey, N NewValue, N CurrentValue);

    private readonly string insert = $"insert into seq (hashkey, value) values (@HashKey, @Value)";
    private readonly string update = $"update seq set value = @NewValue where hashkey = @HashKey and value = @CurrentValue";
    private Task<int> InsertOrUpdate((bool exists, N currentValue, N newValue) n, IDbTransaction transaction) =>
        n.exists
            ? connection.ExecuteAsync(update, new UpdateParam(readParam.HashKey, n.newValue, n.currentValue), transaction)
            : connection.ExecuteAsync(insert, new InsertParam(readParam.HashKey, n.newValue), transaction);
}
