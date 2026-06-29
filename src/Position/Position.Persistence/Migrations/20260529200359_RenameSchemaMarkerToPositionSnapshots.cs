using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Position.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToPositionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "position",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "position",
                newName: "position_snapshots",
                newSchema: "position");

            migrationBuilder.AddPrimaryKey(
                name: "PK_position_snapshots",
                schema: "position",
                table: "position_snapshots",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_position_snapshots",
                schema: "position",
                table: "position_snapshots");

            migrationBuilder.RenameTable(
                name: "position_snapshots",
                schema: "position",
                newName: "__schema_marker",
                newSchema: "position");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "position",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
