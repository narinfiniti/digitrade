using BffOrchestratorService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BffOrchestratorService.Persistence.EntityTypeConfigs;

internal sealed class ProcessQueueItemEntityTypeConfiguration : IEntityTypeConfiguration<ProcessQueueItem>
{
    public void Configure(EntityTypeBuilder<ProcessQueueItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "process_queue",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_process_queue_flow_type",
                    "flow_type in ('synchronous', 'asynchronous')");
                tableBuilder.HasCheckConstraint(
                    "ck_process_queue_work_type",
                    "work_type in ('start', 'retry', 'resume', 'timeout', 'cancel_reservations', 'event_observation', 'client_decision', 'escalation')");
                tableBuilder.HasCheckConstraint(
                    "ck_process_queue_status",
                    "status in ('ready', 'leased', 'completed', 'dead_letter', 'cancelled')");
                tableBuilder.HasCheckConstraint(
                    "ck_process_queue_attempt_count",
                    "attempt_count >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_process_queue_max_attempt_count",
                    "max_attempt_count >= 0");
            });

        builder.Ignore(queueItem => queueItem.Version);
        builder.Ignore(queueItem => queueItem.DomainEvents);

        builder.HasKey(queueItem => queueItem.Id);

        builder.Property(queueItem => queueItem.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(queueItem => queueItem.ProcessId)
            .HasColumnName("process_id")
            .IsRequired();

        builder.Property(queueItem => queueItem.ProcessKey)
            .HasColumnName("process_key")
            .IsRequired();

        builder.Property(queueItem => queueItem.FlowType)
            .HasColumnName("flow_type")
            .IsRequired();

        builder.Property(queueItem => queueItem.WorkType)
            .HasColumnName("work_type")
            .IsRequired();

        builder.Property(queueItem => queueItem.Status)
            .HasColumnName("status")
            .HasDefaultValue("ready")
            .IsRequired();

        builder.Property(queueItem => queueItem.Priority)
            .HasColumnName("priority")
            .HasDefaultValue((short)100)
            .IsRequired();

        builder.Property(queueItem => queueItem.VisibleAt)
            .HasColumnName("visible_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(queueItem => queueItem.SequenceNo)
            .HasColumnName("sequence_no")
            .UseIdentityAlwaysColumn()
            .IsRequired();

        builder.Property(queueItem => queueItem.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(queueItem => queueItem.MaxAttemptCount)
            .HasColumnName("max_attempt_count")
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(queueItem => queueItem.LeaseOwner)
            .HasColumnName("lease_owner");

        builder.Property(queueItem => queueItem.LeaseAcquiredAt)
            .HasColumnName("lease_acquired_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(queueItem => queueItem.LeaseExpiresAt)
            .HasColumnName("lease_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(queueItem => queueItem.DedupeKey)
            .HasColumnName("dedupe_key")
            .IsRequired();

        builder.Property(queueItem => queueItem.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(queueItem => queueItem.LastErrorCode)
            .HasColumnName("last_error_code");

        builder.Property(queueItem => queueItem.LastErrorMessage)
            .HasColumnName("last_error_message");

        builder.Property(queueItem => queueItem.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(queueItem => queueItem.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(queueItem => queueItem.DedupeKey)
            .HasDatabaseName("ux_process_queue_dedupe_key")
            .IsUnique();

        builder.HasIndex(queueItem => new { queueItem.Status, queueItem.VisibleAt, queueItem.Priority, queueItem.SequenceNo })
            .HasDatabaseName("ix_process_queue_ready_dequeue")
            .HasFilter("status = 'ready'");

        builder.HasIndex(queueItem => new { queueItem.ProcessKey, queueItem.Status, queueItem.Priority, queueItem.VisibleAt, queueItem.SequenceNo })
            .HasDatabaseName("ix_process_queue_process_key_head");

        builder.HasIndex(queueItem => queueItem.LeaseExpiresAt)
            .HasDatabaseName("ix_process_queue_lease_expiry")
            .HasFilter("status = 'leased'");
    }
}