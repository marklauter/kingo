using System.Runtime.CompilerServices;

namespace Kingo;

public sealed class AsyncLock
    : IDisposable
{
    private bool disposed;
    private readonly SemaphoreSlim latch = new(1, 1);

    public sealed class Token
        : IDisposable
    {
        private readonly SemaphoreSlim latch;

        internal Token(SemaphoreSlim latch) => this.latch = latch;

        public void Dispose() => _ = latch.Release();
    }

    public async Task<Token> LockAsync(CancellationToken cancellationToken)
    {
        await ThrowIfDisposed().latch.WaitAsync(cancellationToken);
        return new Token(latch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<Token> LockAsync() => LockAsync(CancellationToken.None);

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
