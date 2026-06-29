using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trade.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTradeSchema : Migration
    {
        private static readonly string[] OutboxStatusOccurredAtColumns = ["Status", "OccurredAtUtc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "trade");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "trade",
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

            migrationBuilder.CreateTable(
                name: "trades",
                schema: "trade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InstrumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "numeric(18,10)", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "numeric(18,10)", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "trade",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_OccurredAtUtc",
                schema: "trade",
                table: "outbox_messages",
                columns: OutboxStatusOccurredAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_trades_AccountId",
                schema: "trade",
                table: "trades",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_trades_InstrumentId",
                schema: "trade",
                table: "trades",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_trades_Status",
                schema: "trade",
                table: "trades",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "trade");

            migrationBuilder.DropTable(
                name: "trades",
                schema: "trade");
        }
    }
}
