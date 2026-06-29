using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BffNotificationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameNotificationDeliveriesToDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "bffnotification",
                table: "notification_deliveries");

            migrationBuilder.RenameTable(
                name: "notification_deliveries",
                schema: "bffnotification",
                newName: "deliveries",
                newSchema: "bffnotification");

            migrationBuilder.AddPrimaryKey(
                name: "PK_deliveries",
                schema: "bffnotification",
                table: "deliveries",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_deliveries",
                schema: "bffnotification",
                table: "deliveries");

            migrationBuilder.RenameTable(
                name: "deliveries",
                schema: "bffnotification",
                newName: "notification_deliveries",
                newSchema: "bffnotification");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification_deliveries",
                schema: "bffnotification",
                table: "notification_deliveries",
                column: "Id");
        }
    }
}
