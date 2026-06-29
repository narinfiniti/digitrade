using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace Settlement.Persistence;

internal static class SettlementConcurrencyRetryPolicy
{
    private static readonly AsyncRetryPolicy<int> SaveChangesRetryPolicy = Policy<int>
        .Handle<DbUpdateConcurrencyException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(50d * Math.Pow(2d, retryAttempt - 1)));

    public static Task<int> ExecuteAsync(
        Func<CancellationToken, Task<int>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return SaveChangesRetryPolicy.ExecuteAsync(operation, cancellationToken);
    }
}