using Microsoft.EntityFrameworkCore;
using Order.Domain.Orders;
using Order.Persistence;
using Order.Persistence.Orders;
using Xunit;

namespace Order.Tests;

public sealed class OrderPersistenceTests
{
    [Fact]
    public void OrderVersionPropertyIsMarkedAsConcurrencyToken()
    {
        using var dbContext = CreateDbContext($"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-order-model-{Guid.NewGuid():N}.db")}");

        var entityType = dbContext.Model.FindEntityType(typeof(Order.Domain.Orders.Order));
        var versionProperty = entityType?.FindProperty(nameof(Order.Domain.Orders.Order.Version));

        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }

    [Fact]
    public async Task SaveChangesAsyncWhenOrderVersionIsStaleRejectsTheWrite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"digitrade-order-conflict-{Guid.NewGuid():N}.db");

        var orderService = new OrderService();
        var submittedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);
        var initialOrder = orderService.Place("acct-conflict", "EURUSD", OrderDirection.Buy, 1.0m, 1.20000m, submittedAtUtc);

        await using (var seedContext = CreateDbContext($"Data Source={databasePath}"))
        {
            await seedContext.Database.EnsureDeletedAsync();
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.Orders.Add(initialOrder);
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = CreateDbContext($"Data Source={databasePath}");
        await using var secondContext = CreateDbContext($"Data Source={databasePath}");

        var firstRepository = new OrderRepository(firstContext);
        var secondRepository = new OrderRepository(secondContext);

        var firstCopy = await firstRepository.FindByIdAsync(initialOrder.Id);
        var staleCopy = await secondRepository.FindByIdAsync(initialOrder.Id);

        Assert.NotNull(firstCopy);
        Assert.NotNull(staleCopy);

        orderService.Accept(firstCopy!, submittedAtUtc.AddMinutes(5));
        await firstRepository.SaveChangesAsync();

        orderService.Reject(staleCopy!, submittedAtUtc.AddMinutes(6));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRepository.SaveChangesAsync());
    }

    private static OrderDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new OrderDbContext(options);
    }
}