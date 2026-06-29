using BffOrchestratorService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BffOrchestratorService.Persistence.EntityTypeConfigs;

internal sealed class BusinessProcessStateEntityTypeConfiguration : IEntityTypeConfiguration<BusinessProcessState>
{
    public void Configure(EntityTypeBuilder<BusinessProcessState> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "business_process_state",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_flow_type",
                    "flow_type in ('synchronous', 'asynchronous')");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_status",
                    "status in ('started', 'in_progress', 'waiting', 'interrupted', 'retrying', 'compensating', 'completed', 'failed', 'escalated', 'paused')");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_recovery_policy",
                    "recovery_policy in ('resume_on_restart', 'alert_client_and_pause', 'escalate_and_stop')");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_version",
                    "version > 0");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_current_step_ordinal",
                    "current_step_ordinal >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_retry_count",
                    "retry_count >= 0");
                tableBuilder.HasCheckConstraint(
                    "ck_business_process_state_max_retry_count",
                    "max_retry_count >= 0");
            });

            builder.Ignore(state => state.DomainEvents);

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(state => state.ProcessName)
            .HasColumnName("process_name")
            .IsRequired();

        builder.Property(state => state.ProcessKey)
            .HasColumnName("process_key")
            .IsRequired();

        builder.Property(state => state.AggregateId)
            .HasColumnName("aggregate_id");

        builder.Property(state => state.FlowType)
            .HasColumnName("flow_type")
            .IsRequired();

        builder.Property(state => state.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(state => state.CurrentStepOrdinal)
            .HasColumnName("current_step_ordinal")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(state => state.CurrentStepName)
            .HasColumnName("current_step_name")
            .HasDefaultValue("created")
            .IsRequired();

        builder.Property(state => state.Version)
            .HasColumnName("version")
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(state => state.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired();

        builder.Property(state => state.CorrelationId)
            .HasColumnName("correlation_id")
            .IsRequired();

        builder.Property(state => state.CausationId)
            .HasColumnName("causation_id");

        builder.Property(state => state.IntentPersistedAt)
            .HasColumnName("intent_persisted_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(state => state.SyncDeadlineAt)
            .HasColumnName("sync_deadline_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.RecoveryPolicy)
            .HasColumnName("recovery_policy")
            .IsRequired();

        builder.Property(state => state.AwaitingClientDecision)
            .HasColumnName("awaiting_client_decision")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(state => state.ClientDecisionDeadlineAt)
            .HasColumnName("client_decision_deadline_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(state => state.MaxRetryCount)
            .HasColumnName("max_retry_count")
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(state => state.NextVisibleAt)
            .HasColumnName("next_visible_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(state => state.LeaseOwner)
            .HasColumnName("lease_owner");

        builder.Property(state => state.LeaseAcquiredAt)
            .HasColumnName("lease_acquired_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.LeaseExpiresAt)
            .HasColumnName("lease_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.HeartbeatAt)
            .HasColumnName("heartbeat_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.InterruptedAt)
            .HasColumnName("interrupted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.ResponseStatusCode)
            .HasColumnName("response_status_code");

        builder.Property(state => state.ResponseCommittedAt)
            .HasColumnName("response_committed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(state => state.LastErrorCode)
            .HasColumnName("last_error_code");

        builder.Property(state => state.LastErrorMessage)
            .HasColumnName("last_error_message");

        builder.Property(state => state.ProcessContext)
            .HasColumnName("process_context")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(state => state.InputPayload)
            .HasColumnName("input_payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(state => state.ResultPayload)
            .HasColumnName("result_payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(state => state.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(state => state.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(state => new { state.ProcessName, state.IdempotencyKey })
            .HasDatabaseName("ux_business_process_state_name_idempotency")
            .IsUnique();

        builder.HasIndex(state => new { state.ProcessKey, state.NextVisibleAt, state.Id })
            .HasDatabaseName("ix_business_process_state_process_key_active")
            .HasFilter("status in ('started', 'in_progress', 'waiting', 'retrying', 'compensating', 'interrupted', 'paused')");

        builder.HasIndex(state => state.LeaseExpiresAt)
            .HasDatabaseName("ix_business_process_state_lease_expiry")
            .HasFilter("lease_expires_at is not null");

        builder.HasIndex(state => state.CorrelationId)
            .HasDatabaseName("ix_business_process_state_correlation");

        builder.HasMany<ProcessCheckpoint>()
            .WithOne()
            .HasForeignKey(checkpoint => checkpoint.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<ProcessQueueItem>()
            .WithOne()
            .HasForeignKey(queueItem => queueItem.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
