using Microsoft.EntityFrameworkCore;
using Risk.Domain.Margins;
using Risk.Persistence;
using Risk.Persistence.Margins;
using Xunit;

namespace Risk.Tests;

public sealed class RiskPersistenceTests
{
    [Fact]
    public void MarginAccountVersionPropertyIsMarkedAsConcurrencyToken()
    {
        using var dbContext = CreateDbContext($"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-risk-model-{Guid.NewGuid():N}.db")}");

        var entityType = dbContext.Model.FindEntityType(typeof(Risk.Domain.Margins.MarginAccount));
        var versionProperty = entityType?.FindProperty(nameof(Risk.Domain.Margins.MarginAccount.Version));

        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }

    [Fact]
    public async Task SaveChangesAsyncWhenMarginAccountVersionIsStaleRejectsTheWrite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"digitrade-risk-conflict-{Guid.NewGuid():N}.db");

        var marginService = new MarginService();
        var openedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);
        var initialMarginAccount = marginService.Open("acct-conflict", "USD", 1000m, openedAtUtc);

        await using (var seedContext = CreateDbContext($"Data Source={databasePath}"))
        {
            await seedContext.Database.EnsureDeletedAsync();
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.MarginAccounts.Add(initialMarginAccount);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext($"Data Source={databasePath}");
        await using var secondContext = CreateDbContext($"Data Source={databasePath}");

        var firstRepository = new MarginAccountRepository(firstContext);
        var secondRepository = new MarginAccountRepository(secondContext);

        var firstCopy = await firstRepository.FindByIdAsync(initialMarginAccount.Id);
        var staleCopy = await secondRepository.FindByIdAsync(initialMarginAccount.Id);

        Assert.NotNull(firstCopy);
        Assert.NotNull(staleCopy);

        marginService.Reserve(firstCopy!, 250m, openedAtUtc.AddMinutes(5));
        await firstRepository.SaveChangesAsync();

        marginService.Reserve(staleCopy!, 100m, openedAtUtc.AddMinutes(6));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRepository.SaveChangesAsync());
    }

    private static RiskDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<RiskDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new RiskDbContext(options);
    }
}