using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixChecklistSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ss_checklist_proyecto_proyecto_id",
                table: "ss_checklist_proyecto");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_proyecto_id_plantilla_id",
                table: "ss_checklist_proyecto",
                columns: new[] { "proyecto_id", "plantilla_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ss_checklist_proyecto_proyecto_id_plantilla_id",
                table: "ss_checklist_proyecto");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_proyecto_id",
                table: "ss_checklist_proyecto",
                column: "proyecto_id");
        }
    }
}
