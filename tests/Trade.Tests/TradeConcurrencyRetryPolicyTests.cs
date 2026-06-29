using Microsoft.EntityFrameworkCore;
using Trade.Persistence;
using Xunit;

namespace Trade.Tests;

public sealed class TradeConcurrencyRetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsyncRetriesConcurrencyFailuresThreeTimesBeforeThrowing()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => TradeConcurrencyRetryPolicy.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.FromException<int>(new DbUpdateConcurrencyException("Simulated optimistic concurrency conflict."));
            }));

        Assert.Equal(4, attempts);
    }
}