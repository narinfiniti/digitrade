using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Ledger.Application.Abstractions;
using Ledger.Dependency;
using Ledger.Domain.Ledgers;
using Ledger.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Settlement.Application.Events;
using System.Text.Json;
using Ledger.Persistence.Ledgers;
using Xunit;

namespace Ledger.Tests;

public sealed class LedgerServiceTests
{
    private readonly LedgerService subject = new();

    private sealed class TestIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly List<IntegrationEnvelope> publishedEnvelopes = [];

        public IReadOnlyCollection<IntegrationEnvelope> PublishedEnvelopes => publishedEnvelopes;

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);

            publishedEnvelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingIntegrationEventPublisher(string failureMessage) : IIntegrationEventPublisher
    {
        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);

            throw new InvalidOperationException(failureMessage);
        }
    }

    [Fact]
    public void PostAssignsBalancedEntryAndEmitsPostedEvent()
    {
        var settlementId = Guid.NewGuid();
        var postedAtUtc = new DateTimeOffset(2026, 05, 29, 16, 00, 00, TimeSpan.Zero);

        var ledgerEntry = subject.Post(
            settlementId,
            " acct-1 ",
            " usd ",
            [
                new LedgerPostingInstruction(" cash ", LedgerPostingSide.Debit, 125.50m),
                new LedgerPostingInstruction(" revenue ", LedgerPostingSide.Credit, 125.50m),
            ],
            postedAtUtc);

        Assert.NotEqual(Guid.Empty, ledgerEntry.Id);
        Assert.Equal(settlementId, ledgerEntry.SettlementId);
        Assert.Equal("acct-1", ledgerEntry.AccountId);
        Assert.Equal("USD", ledgerEntry.CurrencyCode);
        Assert.Equal(125.50m, ledgerEntry.TotalAmount);
        Assert.Equal(postedAtUtc, ledgerEntry.PostedAtUtc);
        Assert.Equal(1, ledgerEntry.Version);
        Assert.Equal(2, ledgerEntry.PostingLines.Count);

        Assert.Collection(
            ledgerEntry.PostingLines,
            debitLine =>
            {
                Assert.Equal("CASH", debitLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Debit, debitLine.Side);
                Assert.Equal(125.50m, debitLine.Amount);
            },
            creditLine =>
            {
                Assert.Equal("REVENUE", creditLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Credit, creditLine.Side);
                Assert.Equal(125.50m, creditLine.Amount);
            });

        var domainEvent = Assert.Single(ledgerEntry.DomainEvents.OfType<LedgerEntryPostedDomainEvent>());
        Assert.Equal(ledgerEntry.Id, domainEvent.LedgerEntryId);
        Assert.Equal(settlementId, domainEvent.SettlementId);
        Assert.Equal("acct-1", domainEvent.AccountId);
        Assert.Equal("USD", domainEvent.CurrencyCode);
        Assert.Equal(125.50m, domainEvent.TotalAmount);
        Assert.Equal(2, domainEvent.PostingLineCount);
        Assert.Equal(postedAtUtc, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void PostWithUnbalancedPostingLinesThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => subject.Post(
            Guid.NewGuid(),
            "acct-1",
            "USD",
            [
                new LedgerPostingInstruction("cash", LedgerPostingSide.Debit, 100m),
                new LedgerPostingInstruction("revenue", LedgerPostingSide.Credit, 95m),
            ],
            new DateTimeOffset(2026, 05, 29, 16, 00, 00, TimeSpan.Zero)));

        Assert.Equal("postingLines", exception.ParamName);
    }

    [Fact]
    public void PostWithSingleSidedLinesThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => subject.Post(
            Guid.NewGuid(),
            "acct-1",
            "USD",
            [
                new LedgerPostingInstruction("cash", LedgerPostingSide.Debit, 100m),
                new LedgerPostingInstruction("receivable", LedgerPostingSide.Debit, 100m),
            ],
            new DateTimeOffset(2026, 05, 29, 16, 00, 00, TimeSpan.Zero)));

        Assert.Equal("postingLines", exception.ParamName);
    }

    [Fact]
    public async Task PersistedLedgerEntryRoundTripsPostingLinesAndVersionMetadata()
    {
        var settlementId = Guid.NewGuid();
        var postedAtUtc = new DateTimeOffset(2026, 05, 29, 16, 30, 00, TimeSpan.Zero);
        var ledgerEntry = subject.Post(
            settlementId,
            "acct-1",
            "USD",
            [
                new LedgerPostingInstruction("cash", LedgerPostingSide.Debit, 210.25m),
                new LedgerPostingInstruction("clearing", LedgerPostingSide.Credit, 210.25m),
            ],
            postedAtUtc);

        var connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-ledger-tests-{Guid.NewGuid():N}.db")}";
        using var dbContext = CreateDbContext(connectionString);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new LedgerRepository(dbContext);
        await repository.AddAsync(ledgerEntry);
        await repository.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedEntry = await repository.FindBySettlementIdAsync(settlementId);

        Assert.NotNull(persistedEntry);
        Assert.Equal(ledgerEntry.Id, persistedEntry!.Id);
        Assert.Equal("acct-1", persistedEntry.AccountId);
        Assert.Equal("USD", persistedEntry.CurrencyCode);
        Assert.Equal(210.25m, persistedEntry.TotalAmount);
        Assert.Equal(postedAtUtc, persistedEntry.PostedAtUtc);
        Assert.Equal(1, persistedEntry.Version);

        Assert.Collection(
            persistedEntry.PostingLines,
            debitLine =>
            {
                Assert.Equal("CASH", debitLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Debit, debitLine.Side);
                Assert.Equal(210.25m, debitLine.Amount);
            },
            creditLine =>
            {
                Assert.Equal("CLEARING", creditLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Credit, creditLine.Side);
                Assert.Equal(210.25m, creditLine.Amount);
            });

        var entityType = dbContext.Model.FindEntityType(typeof(LedgerEntry));
        var versionProperty = entityType?.FindProperty(nameof(LedgerEntry.Version));

        Assert.NotNull(versionProperty);
        Assert.True(versionProperty!.IsConcurrencyToken);
    }

    private static LedgerDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new LedgerDbContext(options);
    }

    [Fact]
    public async Task SettlementFinalizedConsumerPersistsOneLedgerEntryPerSettlement()
    {
        var connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-ledger-consumer-{Guid.NewGuid():N}.db")}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LEDGER_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Ledger"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDependencyServices(configuration);
        services.RemoveAll<IDbContextOptionsConfiguration<LedgerDbContext>>();
        services.RemoveAll<DbContextOptions<LedgerDbContext>>();
        services.RemoveAll<LedgerDbContext>();
        services.RemoveAll<IIntegrationEventPublisher>();
        services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<TestIntegrationEventPublisher>();
        services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<TestIntegrationEventPublisher>());

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var consumer = scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer<SettlementFinalizedIntegrationEvent>>();
        var outboxPublisher = scope.ServiceProvider.GetRequiredService<ILedgerOutboxPublisher>();
        var integrationEventPublisher = scope.ServiceProvider.GetRequiredService<TestIntegrationEventPublisher>();
        var finalizedEvent = new SettlementFinalizedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            "acct-1",
            "USD",
            -42.75m,
            new DateTimeOffset(2026, 05, 29, 17, 00, 00, TimeSpan.Zero));

        await consumer.ConsumeAsync(finalizedEvent);
        await consumer.ConsumeAsync(finalizedEvent);
        await outboxPublisher.PublishPendingAsync();

        var persistedEntries = await dbContext.LedgerEntries.ToListAsync();

        var ledgerEntry = Assert.Single(persistedEntries);
        Assert.Equal(Guid.Parse(finalizedEvent.AggregateId), ledgerEntry.SettlementId);
        Assert.Equal("acct-1", ledgerEntry.AccountId);
        Assert.Equal("USD", ledgerEntry.CurrencyCode);
        Assert.Equal(42.75m, ledgerEntry.TotalAmount);
        Assert.Equal(1, ledgerEntry.Version);

        var outboxMessage = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
        Assert.Equal("ledger.entry.posted", outboxMessage.EventName);
    Assert.Equal(OutboxMessageStatus.Published, outboxMessage.Status);
        Assert.Equal(ledgerEntry.Id.ToString("D"), outboxMessage.AggregateId);
    Assert.NotNull(outboxMessage.PublishedAtUtc);

    var publishedEnvelope = Assert.Single(integrationEventPublisher.PublishedEnvelopes);
    Assert.Equal("ledger.entry.posted", publishedEnvelope.IntegrationEvent.EventName);
    Assert.Equal(ledgerEntry.Id.ToString("D"), publishedEnvelope.IntegrationEvent.AggregateId);
    Assert.Equal(outboxMessage.Id.ToString("D"), publishedEnvelope.Headers["outbox-message-id"]);

        using var payloadDocument = JsonDocument.Parse(outboxMessage.Payload);
        Assert.Equal(ledgerEntry.Id.ToString("D"), payloadDocument.RootElement.GetProperty("aggregateId").GetString());
        Assert.Equal(finalizedEvent.AggregateId, payloadDocument.RootElement.GetProperty("settlementId").GetString());
        Assert.Equal("acct-1", payloadDocument.RootElement.GetProperty("accountId").GetString());
        Assert.Equal("USD", payloadDocument.RootElement.GetProperty("currencyCode").GetString());
        Assert.Equal(42.75m, payloadDocument.RootElement.GetProperty("totalAmount").GetDecimal());

        Assert.Collection(
            ledgerEntry.PostingLines,
            debitLine =>
            {
                Assert.Equal("SETTLEMENT_CLEARING", debitLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Debit, debitLine.Side);
                Assert.Equal(42.75m, debitLine.Amount);
            },
            creditLine =>
            {
                Assert.Equal("CUSTOMER_SETTLEMENT_BALANCE", creditLine.AccountCode);
                Assert.Equal(LedgerPostingSide.Credit, creditLine.Side);
                Assert.Equal(42.75m, creditLine.Amount);
            });
    }

    [Fact]
    public async Task LedgerOutboxPublisherMarksMessageFailedWhenPublishThrows()
    {
        var connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"digitrade-ledger-publish-failure-{Guid.NewGuid():N}.db")}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LEDGER_DB_CONNECTION"] = connectionString,
                ["ConnectionStrings:Ledger"] = connectionString,
            })
            .Build();

        var failureMessage = "simulated ledger publish failure";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDependencyServices(configuration);
        services.RemoveAll<IDbContextOptionsConfiguration<LedgerDbContext>>();
        services.RemoveAll<DbContextOptions<LedgerDbContext>>();
        services.RemoveAll<LedgerDbContext>();
        services.RemoveAll<IIntegrationEventPublisher>();
        services.AddDbContext<LedgerDbContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<IIntegrationEventPublisher>(new ThrowingIntegrationEventPublisher(failureMessage));

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var consumer = scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer<SettlementFinalizedIntegrationEvent>>();
        var outboxPublisher = scope.ServiceProvider.GetRequiredService<ILedgerOutboxPublisher>();
        var finalizedEvent = new SettlementFinalizedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            "acct-2",
            "EUR",
            88.10m,
            new DateTimeOffset(2026, 05, 29, 18, 00, 00, TimeSpan.Zero));

        await consumer.ConsumeAsync(finalizedEvent);
        await outboxPublisher.PublishPendingAsync();

        var ledgerEntry = Assert.Single(await dbContext.LedgerEntries.ToListAsync());
        Assert.Equal(Guid.Parse(finalizedEvent.AggregateId), ledgerEntry.SettlementId);
        Assert.Equal(88.10m, ledgerEntry.TotalAmount);

        var outboxMessage = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
        Assert.Equal("ledger.entry.posted", outboxMessage.EventName);
        Assert.Equal(OutboxMessageStatus.Failed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.AttemptCount);
        Assert.Equal(failureMessage, outboxMessage.FailureReason);
        Assert.NotNull(outboxMessage.LastAttemptAtUtc);
        Assert.Null(outboxMessage.PublishedAtUtc);
    }
}