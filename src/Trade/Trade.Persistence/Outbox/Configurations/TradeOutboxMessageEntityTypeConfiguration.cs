using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trade.Persistence.Outbox.Configurations;

public sealed class TradeOutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<TradeOutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<TradeOutboxMessageEntity> builder)
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

        builder.Property(message => message.EventVersion)
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.HeadersJson)
            .HasColumnType("jsonb");

        builder.Property(message => message.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(message => message.LastAttemptAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.PublishedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(message => message.FailureReason)
            .HasColumnType("text");

        builder.HasIndex(message => message.EventId)
            .IsUnique();

        builder.HasIndex(message => new { message.Status, message.OccurredAtUtc });
    }
}