using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BffOrchestratorService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProcessRuntimeOutbox : Migration
    {
        private static readonly string[] StatusOccurredAtColumns = ["Status", "OccurredAtUtc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PartitionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    HeadersJson = table.Column<string>(type: "jsonb", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_OccurredAtUtc",
                table: "outbox_messages",
                columns: StatusOccurredAtColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
