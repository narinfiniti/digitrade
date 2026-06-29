using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BffOrchestratorService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDurableProcessRuntime : Migration
    {
        private static readonly string[] BusinessProcessStateActiveColumns = ["process_key", "next_visible_at", "Id"];
        private static readonly string[] BusinessProcessStateNameIdempotencyColumns = ["process_name", "idempotency_key"];
        private static readonly string[] ProcessCheckpointStepColumns = ["process_id", "step_ordinal", "created_at"];
        private static readonly string[] ProcessCheckpointKindIdempotencyColumns = ["process_id", "checkpoint_kind", "idempotency_key"];
        private static readonly string[] ProcessQueueProcessKeyHeadColumns = ["process_key", "status", "priority", "visible_at", "sequence_no"];
        private static readonly string[] ProcessQueueReadyDequeueColumns = ["status", "visible_at", "priority", "sequence_no"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_process_state",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_name = table.Column<string>(type: "text", nullable: false),
                    process_key = table.Column<string>(type: "text", nullable: false),
                    aggregate_id = table.Column<string>(type: "text", nullable: true),
                    flow_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    current_step_ordinal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_step_name = table.Column<string>(type: "text", nullable: false, defaultValue: "created"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<string>(type: "text", nullable: false),
                    causation_id = table.Column<string>(type: "text", nullable: true),
                    intent_persisted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    sync_deadline_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recovery_policy = table.Column<string>(type: "text", nullable: false),
                    awaiting_client_decision = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    client_decision_deadline_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    next_visible_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    lease_owner = table.Column<string>(type: "text", nullable: true),
                    lease_acquired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    interrupted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    response_committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "text", nullable: true),
                    last_error_message = table.Column<string>(type: "text", nullable: true),
                    process_context = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    input_payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    result_payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_process_state", x => x.Id);
                    table.CheckConstraint("ck_business_process_state_current_step_ordinal", "current_step_ordinal >= 0");
                    table.CheckConstraint("ck_business_process_state_flow_type", "flow_type in ('synchronous', 'asynchronous')");
                    table.CheckConstraint("ck_business_process_state_max_retry_count", "max_retry_count >= 0");
                    table.CheckConstraint("ck_business_process_state_recovery_policy", "recovery_policy in ('resume_on_restart', 'alert_client_and_pause', 'escalate_and_stop')");
                    table.CheckConstraint("ck_business_process_state_retry_count", "retry_count >= 0");
                    table.CheckConstraint("ck_business_process_state_status", "status in ('started', 'in_progress', 'waiting', 'interrupted', 'retrying', 'compensating', 'completed', 'failed', 'escalated', 'paused')");
                    table.CheckConstraint("ck_business_process_state_version", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "process_checkpoint",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_ordinal = table.Column<int>(type: "integer", nullable: false),
                    step_name = table.Column<string>(type: "text", nullable: false),
                    checkpoint_kind = table.Column<string>(type: "text", nullable: false),
                    observed_outcome = table.Column<string>(type: "text", nullable: false),
                    dispatch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    message_key = table.Column<string>(type: "text", nullable: true),
                    external_reference = table.Column<string>(type: "text", nullable: true),
                    checkpoint_data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_checkpoint", x => x.Id);
                    table.CheckConstraint("ck_process_checkpoint_kind", "checkpoint_kind in ('dispatch', 'reservation_recorded', 'validation_succeeded', 'commit_dispatched', 'commit_succeeded', 'reservation_cancel_dispatched', 'reservation_cancel_succeeded', 'step_failed', 'retry_scheduled', 'timeout', 'paused', 'interrupted', 'client_decision', 'resumed', 'completed', 'cancelled')");
                    table.CheckConstraint("ck_process_checkpoint_observed_outcome", "observed_outcome in ('pending', 'reserved', 'validated', 'committed', 'failed', 'timed_out', 'reservation_cancelled', 'paused', 'interrupted', 'approved', 'rejected', 'cancelled')");
                    table.CheckConstraint("ck_process_checkpoint_step_ordinal", "step_ordinal >= 0");
                    table.ForeignKey(
                        name: "FK_process_checkpoint_business_process_state_process_id",
                        column: x => x.process_id,
                        principalTable: "business_process_state",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_queue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    process_id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_key = table.Column<string>(type: "text", nullable: false),
                    flow_type = table.Column<string>(type: "text", nullable: false),
                    work_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "ready"),
                    priority = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    visible_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    sequence_no = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    lease_owner = table.Column<string>(type: "text", nullable: true),
                    lease_acquired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dedupe_key = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    last_error_code = table.Column<string>(type: "text", nullable: true),
                    last_error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_queue", x => x.Id);
                    table.CheckConstraint("ck_process_queue_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_process_queue_flow_type", "flow_type in ('synchronous', 'asynchronous')");
                    table.CheckConstraint("ck_process_queue_max_attempt_count", "max_attempt_count >= 0");
                    table.CheckConstraint("ck_process_queue_status", "status in ('ready', 'leased', 'completed', 'dead_letter', 'cancelled')");
                    table.CheckConstraint("ck_process_queue_work_type", "work_type in ('start', 'retry', 'resume', 'timeout', 'cancel_reservations', 'event_observation', 'client_decision', 'escalation')");
                    table.ForeignKey(
                        name: "FK_process_queue_business_process_state_process_id",
                        column: x => x.process_id,
                        principalTable: "business_process_state",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_business_process_state_correlation",
                table: "business_process_state",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_process_state_lease_expiry",
                table: "business_process_state",
                column: "lease_expires_at",
                filter: "lease_expires_at is not null");

            migrationBuilder.CreateIndex(
                name: "ix_business_process_state_process_key_active",
                table: "business_process_state",
                columns: BusinessProcessStateActiveColumns,
                filter: "status in ('started', 'in_progress', 'waiting', 'retrying', 'compensating', 'interrupted', 'paused')");

            migrationBuilder.CreateIndex(
                name: "ux_business_process_state_name_idempotency",
                table: "business_process_state",
                columns: BusinessProcessStateNameIdempotencyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_checkpoint_external_reference",
                table: "process_checkpoint",
                column: "external_reference",
                filter: "external_reference is not null");

            migrationBuilder.CreateIndex(
                name: "ix_process_checkpoint_process_step",
                table: "process_checkpoint",
                columns: ProcessCheckpointStepColumns);

            migrationBuilder.CreateIndex(
                name: "ux_process_checkpoint_process_kind_idempotency",
                table: "process_checkpoint",
                columns: ProcessCheckpointKindIdempotencyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_process_queue_lease_expiry",
                table: "process_queue",
                column: "lease_expires_at",
                filter: "status = 'leased'");

            migrationBuilder.CreateIndex(
                name: "IX_process_queue_process_id",
                table: "process_queue",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_queue_process_key_head",
                table: "process_queue",
                columns: ProcessQueueProcessKeyHeadColumns);

            migrationBuilder.CreateIndex(
                name: "ix_process_queue_ready_dequeue",
                table: "process_queue",
                columns: ProcessQueueReadyDequeueColumns,
                filter: "status = 'ready'");

            migrationBuilder.CreateIndex(
                name: "ux_process_queue_dedupe_key",
                table: "process_queue",
                column: "dedupe_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "process_checkpoint");

            migrationBuilder.DropTable(
                name: "process_queue");

            migrationBuilder.DropTable(
                name: "business_process_state");
        }
    }
}
