using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BffNotificationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToNotificationDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "bffnotification",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "bffnotification",
                newName: "notification_deliveries",
                newSchema: "bffnotification");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "bffnotification",
                table: "notification_deliveries",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "bffnotification",
                table: "notification_deliveries");

            migrationBuilder.RenameTable(
                name: "notification_deliveries",
                schema: "bffnotification",
                newName: "__schema_marker",
                newSchema: "bffnotification");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "bffnotification",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
