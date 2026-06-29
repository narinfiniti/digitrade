using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Settlement.Persistence.Outbox.Configurations;

public sealed class SettlementOutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<SettlementOutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<SettlementOutboxMessageEntity> builder)
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
            .IsRequired();

        builder.Property(message => message.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(message => message.FailureReason)
            .HasMaxLength(2048);

        builder.HasIndex(message => message.EventId)
            .IsUnique();

        builder.HasIndex(message => new { message.Status, message.OccurredAtUtc, message.Id });
    }
}