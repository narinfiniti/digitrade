using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToTradeExecutionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "reporting",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "reporting",
                newName: "trade_execution_reports",
                newSchema: "reporting");

            migrationBuilder.AddPrimaryKey(
                name: "PK_trade_execution_reports",
                schema: "reporting",
                table: "trade_execution_reports",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_trade_execution_reports",
                schema: "reporting",
                table: "trade_execution_reports");

            migrationBuilder.RenameTable(
                name: "trade_execution_reports",
                schema: "reporting",
                newName: "__schema_marker",
                newSchema: "reporting");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "reporting",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
