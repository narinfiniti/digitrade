using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reporting.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTradeExecutionReportsToExecutionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_trade_execution_reports",
                schema: "reporting",
                table: "trade_execution_reports");

            migrationBuilder.RenameTable(
                name: "trade_execution_reports",
                schema: "reporting",
                newName: "execution_reports",
                newSchema: "reporting");

            migrationBuilder.AddPrimaryKey(
                name: "PK_execution_reports",
                schema: "reporting",
                table: "execution_reports",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_execution_reports",
                schema: "reporting",
                table: "execution_reports");

            migrationBuilder.RenameTable(
                name: "execution_reports",
                schema: "reporting",
                newName: "trade_execution_reports",
                newSchema: "reporting");

            migrationBuilder.AddPrimaryKey(
                name: "PK_trade_execution_reports",
                schema: "reporting",
                table: "trade_execution_reports",
                column: "Id");
        }
    }
}
