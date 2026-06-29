using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Account.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToAccountProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "account",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "account",
                newName: "account_profiles",
                newSchema: "account");

            migrationBuilder.AddPrimaryKey(
                name: "PK_account_profiles",
                schema: "account",
                table: "account_profiles",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_account_profiles",
                schema: "account",
                table: "account_profiles");

            migrationBuilder.RenameTable(
                name: "account_profiles",
                schema: "account",
                newName: "__schema_marker",
                newSchema: "account");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "account",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
