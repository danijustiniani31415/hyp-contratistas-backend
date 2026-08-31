using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeriadosAndActivityPredecessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_predecessor",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activity_id = table.Column<int>(type: "integer", nullable: false),
                    predecessor_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_predecessor", x => x.id);
                    table.ForeignKey(
                        name: "fk_activity_predecessor_project_activity_activity_id",
                        column: x => x.activity_id,
                        principalTable: "project_activity",
                        principalColumn: "project_activity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_activity_predecessor_project_activity_predecessor_id",
                        column: x => x.predecessor_id,
                        principalTable: "project_activity",
                        principalColumn: "project_activity_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feriados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feriados", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_predecessor_activity_id",
                table: "activity_predecessor",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "ix_activity_predecessor_activity_id_predecessor_id",
                table: "activity_predecessor",
                columns: new[] { "activity_id", "predecessor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_activity_predecessor_predecessor_id",
                table: "activity_predecessor",
                column: "predecessor_id");

            migrationBuilder.CreateIndex(
                name: "ix_feriados_fecha",
                table: "feriados",
                column: "fecha",
                unique: true);

            // Seed de feriados nacionales no laborables de Perú 2024-2026.
            // Semana Santa (movible): 2024 = 28-29 mar, 2025 = 17-18 abr, 2026 = 02-03 abr.
            migrationBuilder.InsertData(
                table: "feriados",
                columns: new[] { "fecha", "descripcion" },
                values: new object[,]
                {
                    // 2024
                    { new DateOnly(2024, 1, 1),   "Año Nuevo" },
                    { new DateOnly(2024, 3, 28),  "Jueves Santo" },
                    { new DateOnly(2024, 3, 29),  "Viernes Santo" },
                    { new DateOnly(2024, 5, 1),   "Día del Trabajo" },
                    { new DateOnly(2024, 6, 7),   "Batalla de Arica y Día de la Bandera" },
                    { new DateOnly(2024, 6, 29),  "San Pedro y San Pablo" },
                    { new DateOnly(2024, 7, 28),  "Fiestas Patrias" },
                    { new DateOnly(2024, 7, 29),  "Fiestas Patrias" },
                    { new DateOnly(2024, 8, 6),   "Batalla de Junín" },
                    { new DateOnly(2024, 8, 30),  "Santa Rosa de Lima" },
                    { new DateOnly(2024, 10, 8),  "Combate de Angamos" },
                    { new DateOnly(2024, 11, 1),  "Todos los Santos" },
                    { new DateOnly(2024, 12, 8),  "Inmaculada Concepción" },
                    { new DateOnly(2024, 12, 9),  "Batalla de Ayacucho" },
                    { new DateOnly(2024, 12, 25), "Navidad" },
                    // 2025
                    { new DateOnly(2025, 1, 1),   "Año Nuevo" },
                    { new DateOnly(2025, 4, 17),  "Jueves Santo" },
                    { new DateOnly(2025, 4, 18),  "Viernes Santo" },
                    { new DateOnly(2025, 5, 1),   "Día del Trabajo" },
                    { new DateOnly(2025, 6, 7),   "Batalla de Arica y Día de la Bandera" },
                    { new DateOnly(2025, 6, 29),  "San Pedro y San Pablo" },
                    { new DateOnly(2025, 7, 28),  "Fiestas Patrias" },
                    { new DateOnly(2025, 7, 29),  "Fiestas Patrias" },
                    { new DateOnly(2025, 8, 6),   "Batalla de Junín" },
                    { new DateOnly(2025, 8, 30),  "Santa Rosa de Lima" },
                    { new DateOnly(2025, 10, 8),  "Combate de Angamos" },
                    { new DateOnly(2025, 11, 1),  "Todos los Santos" },
                    { new DateOnly(2025, 12, 8),  "Inmaculada Concepción" },
                    { new DateOnly(2025, 12, 9),  "Batalla de Ayacucho" },
                    { new DateOnly(2025, 12, 25), "Navidad" },
                    // 2026
                    { new DateOnly(2026, 1, 1),   "Año Nuevo" },
                    { new DateOnly(2026, 4, 2),   "Jueves Santo" },
                    { new DateOnly(2026, 4, 3),   "Viernes Santo" },
                    { new DateOnly(2026, 5, 1),   "Día del Trabajo" },
                    { new DateOnly(2026, 6, 7),   "Batalla de Arica y Día de la Bandera" },
                    { new DateOnly(2026, 6, 29),  "San Pedro y San Pablo" },
                    { new DateOnly(2026, 7, 28),  "Fiestas Patrias" },
                    { new DateOnly(2026, 7, 29),  "Fiestas Patrias" },
                    { new DateOnly(2026, 8, 6),   "Batalla de Junín" },
                    { new DateOnly(2026, 8, 30),  "Santa Rosa de Lima" },
                    { new DateOnly(2026, 10, 8),  "Combate de Angamos" },
                    { new DateOnly(2026, 11, 1),  "Todos los Santos" },
                    { new DateOnly(2026, 12, 8),  "Inmaculada Concepción" },
                    { new DateOnly(2026, 12, 9),  "Batalla de Ayacucho" },
                    { new DateOnly(2026, 12, 25), "Navidad" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_predecessor");

            migrationBuilder.DropTable(
                name: "feriados");
        }
    }
}
