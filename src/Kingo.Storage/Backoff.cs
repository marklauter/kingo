namespace Kingo.Storage;

internal sealed class Backoff(int maxAttempt)
{
    private int attempt;
    private readonly Random random = Random.Shared; // .NET 6+ thread-safe shared instance

    public Task WaitAsync(CancellationToken cancellationToken) =>
        attempt > maxAttempt
            ? throw new TimeoutException($"retries exceeded max {maxAttempt}")
            : Task.Delay(CalculateDelay(), cancellationToken);

    private int CalculateDelay()
    {
        var backoff = (int)Math.Min(Math.Pow(2, attempt++), 64);
        var jitter = random.Next(0, backoff);
        return backoff - jitter;
    }
}
