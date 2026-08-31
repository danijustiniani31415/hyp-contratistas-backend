using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSsProyectoHabilitado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_ev_evaluacion_contratista_detalle_evaluacion_id",
                table: "ev_evaluacion_contratista_detalle",
                newName: "ix_ev_evaluacion_contratista_detalle_evaluacion_contratista_id");

            migrationBuilder.CreateTable(
                name: "ss_proyecto_habilitado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_proyecto_habilitado", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_proyecto_habilitado_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_proyecto_habilitado_proyecto_id",
                table: "ss_proyecto_habilitado",
                column: "proyecto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ss_proyecto_habilitado");

            migrationBuilder.RenameIndex(
                name: "ix_ev_evaluacion_contratista_detalle_evaluacion_contratista_id",
                table: "ev_evaluacion_contratista_detalle",
                newName: "ix_ev_evaluacion_contratista_detalle_evaluacion_id");
        }
    }
}
