using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Risk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRiskSchema : Migration
    {
        private static readonly string[] OutboxStatusOccurredAtIdColumns = ["Status", "OccurredAtUtc", "Id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "risk");

            migrationBuilder.CreateTable(
                name: "margin_accounts",
                schema: "risk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalMargin = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    ReservedMargin = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_margin_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "risk",
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

            migrationBuilder.CreateIndex(
                name: "IX_margin_accounts_AccountId",
                schema: "risk",
                table: "margin_accounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_margin_accounts_CurrencyCode",
                schema: "risk",
                table: "margin_accounts",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "risk",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Status_OccurredAtUtc_Id",
                schema: "risk",
                table: "outbox_messages",
                columns: OutboxStatusOccurredAtIdColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "margin_accounts",
                schema: "risk");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "risk");
        }
    }
}
