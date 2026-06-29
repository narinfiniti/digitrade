using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Settlement.Tests;

public sealed class SettlementPersistenceTests
{
    [Fact]
    public void SettlementVersionPropertyIsMarkedAsConcurrencyToken()
    {
        using var dbContext = CreateDbContext($"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-settlement-model-{Guid.NewGuid():N}.db")}");

        var entityType = dbContext.Model.FindEntityType(typeof(Settlement.Domain.Settlements.Settlement));
        var versionProperty = entityType?.FindProperty(nameof(Settlement.Domain.Settlements.Settlement.Version));

        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }

    private static Settlement.Persistence.SettlementDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<Settlement.Persistence.SettlementDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new Settlement.Persistence.SettlementDbContext(options);
    }
}