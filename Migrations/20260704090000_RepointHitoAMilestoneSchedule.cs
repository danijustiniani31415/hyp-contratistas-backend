using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RepointHitoAMilestoneSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_consumo_linea_ss_material_hito_hito_id",
                table: "ss_consumo_linea");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_presupuesto_personal_hito_ss_material_hito_hito_id",
                table: "ss_presupuesto_personal_hito");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_consumo_linea_milestone_schedule_hito_id",
                table: "ss_consumo_linea",
                column: "hito_id",
                principalTable: "milestone_schedule",
                principalColumn: "milestone_schedule_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_presupuesto_personal_hito_milestone_schedule_hito_id",
                table: "ss_presupuesto_personal_hito",
                column: "hito_id",
                principalTable: "milestone_schedule",
                principalColumn: "milestone_schedule_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_consumo_linea_milestone_schedule_hito_id",
                table: "ss_consumo_linea");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_presupuesto_personal_hito_milestone_schedule_hito_id",
                table: "ss_presupuesto_personal_hito");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_consumo_linea_ss_material_hito_hito_id",
                table: "ss_consumo_linea",
                column: "hito_id",
                principalTable: "ss_material_hito",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_presupuesto_personal_hito_ss_material_hito_hito_id",
                table: "ss_presupuesto_personal_hito",
                column: "hito_id",
                principalTable: "ss_material_hito",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
