using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToPriceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "pricing",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "pricing",
                newName: "price_snapshots",
                newSchema: "pricing");

            migrationBuilder.AddPrimaryKey(
                name: "PK_price_snapshots",
                schema: "pricing",
                table: "price_snapshots",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_price_snapshots",
                schema: "pricing",
                table: "price_snapshots");

            migrationBuilder.RenameTable(
                name: "price_snapshots",
                schema: "pricing",
                newName: "__schema_marker",
                newSchema: "pricing");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "pricing",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
