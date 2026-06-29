using System.Text.Json;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.Persistence.Extensions;
using Ledger.Application.Abstractions;
using Ledger.Domain.Ledgers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Polly;
using Polly.Retry;

namespace Ledger.Persistence.Ledgers;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options, IMediator? mediator = null) : DbContext(options)
{
    private readonly IMediator? mediator = mediator;

    public const string DefaultSchema = "ledger";

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<LedgerOutboxMessageEntity> OutboxMessages => Set<LedgerOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new LedgerEntryEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerOutboxMessageEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        if (mediator is not null)
        {
            await mediator.DispatchDomainEventsAsync(this, cancellationToken);
        }

        return await SaveChangesAsync(cancellationToken);
    }
}

public sealed class LedgerRepository(LedgerDbContext dbContext) : ILedgerEntryRepository
{
    public async Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ledgerEntry);

        await dbContext.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);
    }

    public Task<LedgerEntry?> FindBySettlementIdAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return dbContext.LedgerEntries.SingleOrDefaultAsync(
            ledgerEntry => ledgerEntry.SettlementId == settlementId,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return LedgerConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveChangesAsync(ct),
            cancellationToken);
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return LedgerConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveEntitiesAsync(ct),
            cancellationToken);
    }
}

public sealed class LedgerOutboxStore(LedgerDbContext dbContext) : IOutboxStore
{
    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        dbContext.OutboxMessages.Add(ToEntity(message));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var normalizedBatchSize = Math.Max(batchSize, 1);

        var messages = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(normalizedBatchSize)
            .Select(ToModel)
            .ToArray();
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .SingleAsync(outboxMessage => outboxMessage.Id == messageId, cancellationToken);

        message.Status = OutboxMessageStatus.Published;
        message.AttemptCount += 1;
        message.LastAttemptAtUtc = publishedAtUtc;
        message.PublishedAtUtc = publishedAtUtc;
        message.FailureReason = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid messageId, string failureReason, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .SingleAsync(outboxMessage => outboxMessage.Id == messageId, cancellationToken);

        message.Status = OutboxMessageStatus.Failed;
        message.AttemptCount += 1;
        message.LastAttemptAtUtc = failedAtUtc;
        message.FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Ledger outbox publish failed."
            : failureReason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static LedgerOutboxMessageEntity ToEntity(OutboxMessage message)
    {
        return new LedgerOutboxMessageEntity
        {
            Id = message.MessageId,
            EventId = message.EventId,
            EventName = message.EventName,
            AggregateId = message.AggregateId,
            PartitionKey = message.PartitionKey,
            EventVersion = message.EventVersion,
            OccurredAtUtc = message.OccurredAtUtc,
            Payload = message.Payload,
            HeadersJson = message.HeadersJson,
            TransactionId = message.TransactionId,
            Status = message.Status,
            AttemptCount = message.AttemptCount,
            LastAttemptAtUtc = message.LastAttemptAtUtc,
            PublishedAtUtc = message.PublishedAtUtc,
            FailureReason = message.FailureReason,
        };
    }

    private static OutboxMessage ToModel(LedgerOutboxMessageEntity message)
    {
        return new OutboxMessage(
            message.Id,
            message.EventId,
            message.EventName,
            message.AggregateId,
            message.PartitionKey,
            message.EventVersion,
            message.OccurredAtUtc,
            message.Payload,
            message.HeadersJson,
            message.TransactionId,
            message.Status,
            message.AttemptCount,
            message.LastAttemptAtUtc,
            message.PublishedAtUtc,
            message.FailureReason);
    }
}

internal sealed class LedgerEntryEntityTypeConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<IReadOnlyCollection<LedgerPostingLine>> PostingLinesComparer = new(
        (left, right) => PostingLinesEqual(left, right),
        postingLines => GetPostingLinesHashCode(postingLines),
        postingLines => ClonePostingLines(postingLines));

    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ledger_entries");

        builder.HasKey(ledgerEntry => ledgerEntry.Id);

        builder.Property(ledgerEntry => ledgerEntry.Id)
            .ValueGeneratedNever();

        builder.Property(ledgerEntry => ledgerEntry.SettlementId)
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.AccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.CurrencyCode)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.TotalAmount)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.PostedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        var postingLinesProperty = builder.Property(ledgerEntry => ledgerEntry.PostingLines)
            .HasColumnName("PostingLinesJson")
            .HasColumnType("TEXT")
            .HasConversion(
                postingLines => SerializePostingLines(postingLines),
                serializedPostingLines => DeserializePostingLines(serializedPostingLines))
            .IsRequired();

        postingLinesProperty.Metadata.SetValueComparer(PostingLinesComparer);

        builder.Property(ledgerEntry => ledgerEntry.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(ledgerEntry => ledgerEntry.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(ledgerEntry => ledgerEntry.SettlementId)
            .IsUnique();

        builder.HasIndex(ledgerEntry => ledgerEntry.AccountId);
        builder.HasIndex(ledgerEntry => ledgerEntry.CurrencyCode);

        builder.Ignore(ledgerEntry => ledgerEntry.DomainEvents);
    }

    private static string SerializePostingLines(IReadOnlyCollection<LedgerPostingLine>? postingLines)
    {
        return JsonSerializer.Serialize(postingLines ?? Array.Empty<LedgerPostingLine>(), SerializerOptions);
    }

    private static LedgerPostingLine[] DeserializePostingLines(string? serializedPostingLines)
    {
        if (string.IsNullOrWhiteSpace(serializedPostingLines))
        {
            return Array.Empty<LedgerPostingLine>();
        }

        return JsonSerializer.Deserialize<LedgerPostingLine[]>(serializedPostingLines, SerializerOptions)
            ?? Array.Empty<LedgerPostingLine>();
    }

    private static bool PostingLinesEqual(
        IReadOnlyCollection<LedgerPostingLine>? left,
        IReadOnlyCollection<LedgerPostingLine>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.SequenceEqual(right);
    }

    private static int GetPostingLinesHashCode(IReadOnlyCollection<LedgerPostingLine> postingLines)
    {
        ArgumentNullException.ThrowIfNull(postingLines);

        return postingLines.Aggregate(0, (current, postingLine) => HashCode.Combine(current, postingLine.GetHashCode()));
    }

    private static LedgerPostingLine[] ClonePostingLines(IReadOnlyCollection<LedgerPostingLine>? postingLines)
    {
        return (postingLines ?? Array.Empty<LedgerPostingLine>()).ToArray();
    }
}

public sealed class LedgerOutboxMessageEntity
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string AggregateId { get; set; } = string.Empty;

    public string PartitionKey { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string Payload { get; set; } = string.Empty;

    public string? HeadersJson { get; set; }

    public Guid? TransactionId { get; set; }

    public OutboxMessageStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public string? FailureReason { get; set; }
}

internal sealed class LedgerOutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<LedgerOutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<LedgerOutboxMessageEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(message => message.EventId)
            .IsRequired();

        builder.Property(message => message.EventName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.AggregateId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(message => message.PartitionKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(message => message.HeadersJson)
            .HasColumnType("TEXT");

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(message => message.LastAttemptAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.PublishedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.FailureReason)
            .HasMaxLength(512);

        builder.HasIndex(message => message.EventId)
            .IsUnique();

        builder.HasIndex(message => new { message.Status, message.OccurredAtUtc, message.Id });
    }
}

internal static class LedgerConcurrencyRetryPolicy
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