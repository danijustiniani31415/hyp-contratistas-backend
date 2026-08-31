using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNoAplicaToEvEvaluacionContratista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ev_evaluacion_contratista_contributor_contributor_id",
                table: "ev_evaluacion_contratista");

            migrationBuilder.DropForeignKey(
                name: "fk_ev_evaluacion_contratista_project_proyecto_id",
                table: "ev_evaluacion_contratista");

            migrationBuilder.AddColumn<DateOnly>(
                name: "cumpleanos",
                table: "person",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "proyecto_id",
                table: "ev_evaluacion_contratista",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "contributor_id",
                table: "ev_evaluacion_contratista",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "no_aplica",
                table: "ev_evaluacion_contratista",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "no_aplica_motivo",
                table: "ev_evaluacion_contratista",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ev_evaluacion_contratista_contributor_contributor_id",
                table: "ev_evaluacion_contratista",
                column: "contributor_id",
                principalTable: "contributor",
                principalColumn: "contributor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ev_evaluacion_contratista_project_proyecto_id",
                table: "ev_evaluacion_contratista",
                column: "proyecto_id",
                principalTable: "project",
                principalColumn: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ev_evaluacion_contratista_contributor_contributor_id",
                table: "ev_evaluacion_contratista");

            migrationBuilder.DropForeignKey(
                name: "fk_ev_evaluacion_contratista_project_proyecto_id",
                table: "ev_evaluacion_contratista");

            migrationBuilder.DropColumn(
                name: "cumpleanos",
                table: "person");

            migrationBuilder.DropColumn(
                name: "no_aplica",
                table: "ev_evaluacion_contratista");

            migrationBuilder.DropColumn(
                name: "no_aplica_motivo",
                table: "ev_evaluacion_contratista");

            migrationBuilder.AlterColumn<int>(
                name: "proyecto_id",
                table: "ev_evaluacion_contratista",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "contributor_id",
                table: "ev_evaluacion_contratista",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ev_evaluacion_contratista_contributor_contributor_id",
                table: "ev_evaluacion_contratista",
                column: "contributor_id",
                principalTable: "contributor",
                principalColumn: "contributor_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ev_evaluacion_contratista_project_proyecto_id",
                table: "ev_evaluacion_contratista",
                column: "proyecto_id",
                principalTable: "project",
                principalColumn: "project_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
