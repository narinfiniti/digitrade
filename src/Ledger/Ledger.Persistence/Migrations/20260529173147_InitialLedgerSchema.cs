using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLedgerSchema : Migration
    {
        private static readonly string[] OutboxStatusOccurredAtIdColumns = ["Status", "OccurredAtUtc", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ledger");

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    PostedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostingLinesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PartitionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    HeadersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_AccountId",
                schema: "ledger",
                table: "ledger_entries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_CurrencyCode",
                schema: "ledger",
                table: "ledger_entries",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_SettlementId",
                schema: "ledger",
                table: "ledger_entries",
                column: "SettlementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "ledger",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_OccurredAtUtc_Id",
                schema: "ledger",
                table: "outbox_messages",
                columns: OutboxStatusOccurredAtIdColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "ledger");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ledger");
        }
    }
}
