using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Instrument.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToInstrumentCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "instrument",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "instrument",
                newName: "instrument_catalog",
                newSchema: "instrument");

            migrationBuilder.AddPrimaryKey(
                name: "PK_instrument_catalog",
                schema: "instrument",
                table: "instrument_catalog",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_instrument_catalog",
                schema: "instrument",
                table: "instrument_catalog");

            migrationBuilder.RenameTable(
                name: "instrument_catalog",
                schema: "instrument",
                newName: "__schema_marker",
                newSchema: "instrument");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "instrument",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
