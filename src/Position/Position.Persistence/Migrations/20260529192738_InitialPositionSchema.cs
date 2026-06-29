using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Position.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPositionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "position");

            migrationBuilder.CreateTable(
                name: "__schema_marker",
                schema: "position",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___schema_marker", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__schema_marker",
                schema: "position");
        }
    }
}
