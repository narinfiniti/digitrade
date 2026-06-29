using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BffOrchestratorService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BffOrchestratorModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bfforchestrator");

            migrationBuilder.RenameTable(
                name: "process_queue",
                newName: "process_queue",
                newSchema: "bfforchestrator");

            migrationBuilder.RenameTable(
                name: "process_checkpoint",
                newName: "process_checkpoint",
                newSchema: "bfforchestrator");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                newName: "outbox_messages",
                newSchema: "bfforchestrator");

            migrationBuilder.RenameTable(
                name: "business_process_state",
                newName: "business_process_state",
                newSchema: "bfforchestrator");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "process_queue",
                schema: "bfforchestrator",
                newName: "process_queue");

            migrationBuilder.RenameTable(
                name: "process_checkpoint",
                schema: "bfforchestrator",
                newName: "process_checkpoint");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "bfforchestrator",
                newName: "outbox_messages");

            migrationBuilder.RenameTable(
                name: "business_process_state",
                schema: "bfforchestrator",
                newName: "business_process_state");
        }
    }
}
