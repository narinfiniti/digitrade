using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToPortfolioSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "portfolio",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "portfolio",
                newName: "portfolio_snapshots",
                newSchema: "portfolio");

            migrationBuilder.AddPrimaryKey(
                name: "PK_portfolio_snapshots",
                schema: "portfolio",
                table: "portfolio_snapshots",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_portfolio_snapshots",
                schema: "portfolio",
                table: "portfolio_snapshots");

            migrationBuilder.RenameTable(
                name: "portfolio_snapshots",
                schema: "portfolio",
                newName: "__schema_marker",
                newSchema: "portfolio");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "portfolio",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
