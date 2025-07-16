using Dapper;
using Kingo.Storage.Keys;
using Microsoft.Data.Sqlite;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public sealed class Sequence(
    SqliteConnection connection,
    Key name)
{
    private readonly int batchSize = 100;
    private volatile int filling; // 0 = not filling, 1 = filling
    private readonly long refill = 25;
    private long floor;
    private long ceiling;

    public long Next()
    {
        if (ceiling - floor <= refill)
            Fill(); // kicks off background fill operation

        return Interlocked.Add(ref floor, 1);
    }

    // todo: use optimistic concurrency to get the next [batchSize] block - this just updates the ceiling
    // todo: make durable
    private void Fill()
    {
        if (Interlocked.CompareExchange(ref filling, 1, 0) != 0)
            return;
        try
        {
            // todo: database stuff goes here
            _ = Interlocked.Add(ref ceiling, batchSize);
        }
        finally
        {
            _ = Interlocked.Exchange(ref filling, 0);
        }
    }

    private async Task<long> TryFill()
    {
        var maxBackoff = 1000;
        var retry = 0;
        var backoff = 5;
        while (retry <= 20)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await WriteAsync(await ReadAsync(transaction), transaction) is (true, var n))
                return n;

            await Task.Delay(backoff > maxBackoff ? maxBackoff : backoff, cancellationToken);
            backoff += 5;

            ++retry;
        }

        throw new SequenceException(
            $"retry limit exceeded",
            StorageErrorCodes.RetryLimitExceeded);
    }

    private readonly TransactionManager txman = new(connection);
    private readonly record struct ReadParam(Key HashKey);
    private readonly ReadParam readParam = new(Key.From($"seq/{name}"));

    private readonly string read = $"select value from seq where hashkey = @HashKey";
    private async Task<(bool exists, long currentValue, long newValue)> ReadAsync(IDbTransaction transaction) =>
        MapN((await connection.QuerySingleOrDefaultAsync<long>(read, readParam, transaction))!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (bool exists, long currentValue, long newValue) MapN(long n) =>
        n == 0 ? (false, 0, 1) : (true, n, n + 1);

    private async Task<(bool success, long n)> WriteAsync((bool exists, long currentValue, long newValue) n, IDbTransaction transaction) =>
        await InsertOrUpdate(n, transaction) == 1
            ? (true, n.newValue)
            : (false, n.currentValue);

    private readonly record struct InsertParam(Key HashKey, long Value);
    private readonly record struct UpdateParam(Key HashKey, long NewValue, long CurrentValue);

    private readonly string insert = $"insert into seq (hashkey, value) values (@HashKey, @Value)";
    private readonly string update = $"update seq set value = @NewValue where hashkey = @HashKey and value = @CurrentValue";
    private Task<int> InsertOrUpdate((bool exists, long currentValue, long newValue) n, IDbTransaction transaction) =>
        n.exists
            ? connection.ExecuteAsync(update, new UpdateParam(readParam.HashKey, n.newValue, n.currentValue), transaction)
            : connection.ExecuteAsync(insert, new InsertParam(readParam.HashKey, n.newValue), transaction);
}
