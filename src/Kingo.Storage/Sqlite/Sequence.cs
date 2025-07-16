using Dapper;
using Kingo.Storage.Keys;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public sealed class Sequence(
    SqliteDbContext dbcontext,
    Key name)
    : IDisposable
{
    private readonly int batchSize = 100;
    private readonly AsyncLock gate = new();

    private sealed record Block(long Floor, long Ceiling)
    {
        public Block Next() => new(Floor + 1, Ceiling);
        public bool TriggerFill() => Ceiling - Floor <= 0;
    }

    private Block block = new(0, 0);

    public async ValueTask<long> NextAsync()
    {
        if (block.TriggerFill())
            await FillAsync();

        return Interlocked.Exchange(ref block, block.Next()).Floor;
    }

    private async Task FillAsync()
    {
        using var token = await gate.LockAsync();
        if (!block.TriggerFill())
            return;

        _ = Interlocked.Exchange(ref block, await dbcontext.ExecuteAsync(TryFillAsync, CancellationToken.None));
    }

    // todo: use optimistic concurrency to get the next [batchSize] block - this just updates the ceiling
    // todo: make durable
    private async Task<Block> TryFillAsync(SqliteConnection connection, IDbTransaction transaction)
    {
        while (true)
        {
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

    private readonly record struct ReadParam(Key HashKey);
    private readonly ReadParam readParam = new(Key.From($"seq/{name}"));

    private readonly string read = $"select ceiling from seq where hashkey = @HashKey";
    private async Task<(bool exists, long currentValue, long newValue)> ReadAsync(IDbTransaction transaction) =>
        MapN((await connection.QuerySingleOrDefaultAsync<long?>(read, readParam, transaction))!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (bool exists, long currentValue, long newValue) MapN(long? n) =>
        ((bool exists, long currentValue, long newValue))(n == 0 ? (false, 0, 1) : (true, n, n + 1));

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
    public void Dispose() => gate.Dispose();
}
