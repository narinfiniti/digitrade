using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Settlement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSettlementSchema : Migration
    {
        private static readonly string[] OutboxStatusOccurredAtIdColumns = ["Status", "OccurredAtUtc", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "settlement");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "settlement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PartitionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "settlements",
                schema: "settlement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InitiatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "settlement",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_OccurredAtUtc_Id",
                schema: "settlement",
                table: "outbox_messages",
                columns: OutboxStatusOccurredAtIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_settlements_AccountId",
                schema: "settlement",
                table: "settlements",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_settlements_Status",
                schema: "settlement",
                table: "settlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_settlements_TradeId",
                schema: "settlement",
                table: "settlements",
                column: "TradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "settlement");

            migrationBuilder.DropTable(
                name: "settlements",
                schema: "settlement");
        }
    }
}
