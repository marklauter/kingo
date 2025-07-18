using System.Runtime.CompilerServices;

namespace Kingo;

public sealed class AsyncLock
    : IDisposable
{
    private bool disposed;
    private readonly SemaphoreSlim latch = new(1, 1);

    public sealed class LockToken
        : IDisposable
    {
        private readonly SemaphoreSlim latch;

        internal LockToken(SemaphoreSlim latch) => this.latch = latch;

        public void Dispose() => _ = latch.Release();
    }

    public async Task<LockToken> AcquireAsync(CancellationToken cancellationToken)
    {
        await ThrowIfDisposed().latch.WaitAsync(cancellationToken);
        return new LockToken(latch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<LockToken> AcquireAsync() => AcquireAsync(CancellationToken.None);

    public void Dispose()
    {
        if (disposed)
            return;

        latch.Dispose();

        disposed = true;
    }

    private AsyncLock ThrowIfDisposed() => disposed
        ? throw new ObjectDisposedException(nameof(AsyncLock))
        : this;
}
