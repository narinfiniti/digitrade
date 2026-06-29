using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuditEvidenceRecordsToEvidenceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_evidence_records",
                schema: "audit",
                table: "audit_evidence_records");

            migrationBuilder.RenameTable(
                name: "audit_evidence_records",
                schema: "audit",
                newName: "evidence_records",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "PK_evidence_records",
                schema: "audit",
                table: "evidence_records",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_evidence_records",
                schema: "audit",
                table: "evidence_records");

            migrationBuilder.RenameTable(
                name: "evidence_records",
                schema: "audit",
                newName: "audit_evidence_records",
                newSchema: "audit");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_evidence_records",
                schema: "audit",
                table: "audit_evidence_records",
                column: "Id");
        }
    }
}
