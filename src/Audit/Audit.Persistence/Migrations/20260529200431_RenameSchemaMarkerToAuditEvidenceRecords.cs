using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSchemaMarkerToAuditEvidenceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK___schema_marker",
                schema: "audit",
                table: "__schema_marker");

            migrationBuilder.RenameTable(
                name: "__schema_marker",
                schema: "audit",
                newName: "audit_evidence_records",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_evidence_records",
                schema: "audit",
                table: "audit_evidence_records",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_evidence_records",
                schema: "audit",
                table: "audit_evidence_records");

            migrationBuilder.RenameTable(
                name: "audit_evidence_records",
                schema: "audit",
                newName: "__schema_marker",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "PK___schema_marker",
                schema: "audit",
                table: "__schema_marker",
                column: "Id");
        }
    }
}
