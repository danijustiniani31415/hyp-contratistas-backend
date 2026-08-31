using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerProyectoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTA: el scaffold de EF generó adicionalmente AddColumn a ss_hab_documento_version,
            // CreateTable cat_subarea y CreateTable worker_eventos. Esas operaciones se eliminaron
            // a mano porque son deuda histórica del snapshot — los objetos ya existen en la BD
            // (creados manualmente / por hotfix previos). El snapshot queda actualizado, así que
            // próximas migraciones no las regenerarán.

            migrationBuilder.CreateTable(
                name: "ss_hab_worker_proyecto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    induccion_completada = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_induccion = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_hab_worker_proyecto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_hab_worker_proyecto_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_hab_worker_proyecto_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ss_hab_worker_proyecto_contributor_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "contributor",
                        principalColumn: "contributor_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_hab_worker_proyecto_worker_id",
                table: "ss_hab_worker_proyecto",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_hab_worker_proyecto_proyecto_id",
                table: "ss_hab_worker_proyecto",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_hab_worker_proyecto_empresa_id",
                table: "ss_hab_worker_proyecto",
                column: "empresa_id");

            // Índice único parcial: un worker no puede tener dos asignaciones activas en el mismo proyecto.
            // Permite múltiples proyectos simultáneos (multi-proyecto) y reasignaciones históricas (FechaFin no nula).
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ux_ss_hab_worker_proyecto_activo
                ON ss_hab_worker_proyecto (worker_id, proyecto_id)
                WHERE fecha_fin IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ux_ss_hab_worker_proyecto_activo;");

            migrationBuilder.DropTable(
                name: "ss_hab_worker_proyecto");
        }
    }
}
