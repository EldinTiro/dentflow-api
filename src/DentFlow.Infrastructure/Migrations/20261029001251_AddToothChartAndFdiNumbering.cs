using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddToothChartAndFdiNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tooth_findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tooth = table.Column<int>(type: "integer", nullable: false),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Surfaces = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tooth_findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tooth_findings_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tooth_findings_PatientId",
                table: "tooth_findings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_tooth_findings_TenantId_PatientId",
                table: "tooth_findings",
                columns: new[] { "TenantId", "PatientId" });

            // Tooth numbers were US "Universal" (1–32); the chart and Europe use FDI (11–48).
            // Same mapping as Fdi.FromUniversal. Invoice lines copied from plan items convert too.
            foreach (var table in new[] { "treatment_plan_items", "invoice_line_items" })
            {
                migrationBuilder.Sql($@"
                    UPDATE {table} SET ""ToothNumber"" = CASE
                        WHEN ""ToothNumber"" BETWEEN 1 AND 8   THEN 19 - ""ToothNumber""
                        WHEN ""ToothNumber"" BETWEEN 9 AND 16  THEN 12 + ""ToothNumber""
                        WHEN ""ToothNumber"" BETWEEN 17 AND 24 THEN 55 - ""ToothNumber""
                        WHEN ""ToothNumber"" BETWEEN 25 AND 32 THEN 16 + ""ToothNumber""
                    END
                    WHERE ""ToothNumber"" BETWEEN 1 AND 32;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "treatment_plan_items", "invoice_line_items" })
            {
                migrationBuilder.Sql($@"
                    UPDATE {table} SET ""ToothNumber"" = CASE
                        WHEN ""ToothNumber"" BETWEEN 11 AND 18 THEN 19 - ""ToothNumber""
                        WHEN ""ToothNumber"" BETWEEN 21 AND 28 THEN ""ToothNumber"" - 12
                        WHEN ""ToothNumber"" BETWEEN 31 AND 38 THEN 55 - ""ToothNumber""
                        WHEN ""ToothNumber"" BETWEEN 41 AND 48 THEN ""ToothNumber"" - 16
                        ELSE ""ToothNumber""
                    END;");
            }

            migrationBuilder.DropTable(
                name: "tooth_findings");
        }
    }
}
