using Microsoft.EntityFrameworkCore;
using Trade.Domain.Trades;
using Trade.Persistence;
using Trade.Persistence.Trades;
using Xunit;

namespace Trade.Tests;

public sealed class TradePersistenceTests
{
    [Fact]
    public void TradeVersionPropertyIsMarkedAsConcurrencyToken()
    {
        using var dbContext = CreateDbContext($"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-trade-model-{Guid.NewGuid():N}.db")}");

        var entityType = dbContext.Model.FindEntityType(typeof(Trade.Domain.Trades.Trade));
        var versionProperty = entityType?.FindProperty(nameof(Trade.Domain.Trades.Trade.Version));

        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }

    [Fact]
    public async Task SaveChangesAsyncWhenTradeVersionIsStaleRejectsTheWrite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"digitrade-trade-conflict-{Guid.NewGuid():N}.db");

        var tradeService = new TradeService();
        var openedAtUtc = new DateTimeOffset(2026, 05, 28, 12, 00, 00, TimeSpan.Zero);
        var initialTrade = tradeService.Open("acct-conflict", "EURUSD", TradeDirection.Buy, 1.0m, 1.20000m, openedAtUtc);

        await using (var seedContext = CreateDbContext($"Data Source={databasePath}"))
        {
            await seedContext.Database.EnsureDeletedAsync();
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Trades.Add(initialTrade);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext($"Data Source={databasePath}");
        await using var secondContext = CreateDbContext($"Data Source={databasePath}");

        var firstRepository = new TradeRepository(firstContext);
        var secondRepository = new TradeRepository(secondContext);

        var firstCopy = await firstRepository.FindByIdAsync(initialTrade.Id);
        var staleCopy = await secondRepository.FindByIdAsync(initialTrade.Id);

        Assert.NotNull(firstCopy);
        Assert.NotNull(staleCopy);

        tradeService.Close(firstCopy!, 1.25000m, openedAtUtc.AddMinutes(5));
        await firstRepository.SaveChangesAsync();

        tradeService.Close(staleCopy!, 1.26000m, openedAtUtc.AddMinutes(6));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRepository.SaveChangesAsync());
    }

    private static TradeDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TradeDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new TradeDbContext(options);
    }
}