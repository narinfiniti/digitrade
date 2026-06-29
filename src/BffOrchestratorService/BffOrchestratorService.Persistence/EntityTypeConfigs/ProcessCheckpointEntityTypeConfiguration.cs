using BffOrchestratorService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BffOrchestratorService.Persistence.EntityTypeConfigs;

internal sealed class ProcessCheckpointEntityTypeConfiguration : IEntityTypeConfiguration<ProcessCheckpoint>
{
    public void Configure(EntityTypeBuilder<ProcessCheckpoint> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "process_checkpoint",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_process_checkpoint_step_ordinal",
                    "step_ordinal >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_process_checkpoint_kind",
                    "checkpoint_kind in ('dispatch', 'reservation_recorded', 'validation_succeeded', 'commit_dispatched', 'commit_succeeded', 'reservation_cancel_dispatched', 'reservation_cancel_succeeded', 'step_failed', 'retry_scheduled', 'timeout', 'paused', 'interrupted', 'client_decision', 'resumed', 'completed', 'cancelled')");
                tableBuilder.HasCheckConstraint(
                    "ck_process_checkpoint_observed_outcome",
                    "observed_outcome in ('pending', 'reserved', 'validated', 'committed', 'failed', 'timed_out', 'reservation_cancelled', 'paused', 'interrupted', 'approved', 'rejected', 'cancelled')");
            });

        builder.Ignore(checkpoint => checkpoint.UpdatedAt);
        builder.Ignore(checkpoint => checkpoint.Version);
        builder.Ignore(checkpoint => checkpoint.DomainEvents);

        builder.HasKey(checkpoint => checkpoint.Id);

        builder.Property(checkpoint => checkpoint.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(checkpoint => checkpoint.ProcessId)
            .HasColumnName("process_id")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.StepOrdinal)
            .HasColumnName("step_ordinal")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.StepName)
            .HasColumnName("step_name")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.CheckpointKind)
            .HasColumnName("checkpoint_kind")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.ObservedOutcome)
            .HasColumnName("observed_outcome")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.DispatchId)
            .HasColumnName("dispatch_id")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.MessageKey)
            .HasColumnName("message_key");

        builder.Property(checkpoint => checkpoint.ExternalReference)
            .HasColumnName("external_reference");

        builder.Property(checkpoint => checkpoint.CheckpointData)
            .HasColumnName("checkpoint_data")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(checkpoint => checkpoint.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(checkpoint => new { checkpoint.ProcessId, checkpoint.CheckpointKind, checkpoint.IdempotencyKey })
            .HasDatabaseName("ux_process_checkpoint_process_kind_idempotency")
            .IsUnique();

        builder.HasIndex(checkpoint => new { checkpoint.ProcessId, checkpoint.StepOrdinal, checkpoint.CreatedAt })
            .HasDatabaseName("ix_process_checkpoint_process_step");

        builder.HasIndex(checkpoint => checkpoint.ExternalReference)
            .HasDatabaseName("ix_process_checkpoint_external_reference")
            .HasFilter("external_reference is not null");
    }
}
