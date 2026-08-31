using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAgenteRiesgoYOcupacionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ocupacion_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "agente_riesgo_id",
                table: "ss_accidente_trabajo",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ss_agente_riesgo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_agente_riesgo", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workers_ocupacion_id",
                table: "workers",
                column: "ocupacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_accidente_trabajo_agente_riesgo_id",
                table: "ss_accidente_trabajo",
                column: "agente_riesgo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_accidente_trabajo_ss_agente_riesgo_agente_riesgo_id",
                table: "ss_accidente_trabajo",
                column: "agente_riesgo_id",
                principalTable: "ss_agente_riesgo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_cat_ocupacion_ocupacion_id",
                table: "workers",
                column: "ocupacion_id",
                principalTable: "cat_ocupacion",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_trabajo_ss_agente_riesgo_agente_riesgo_id",
                table: "ss_accidente_trabajo");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_cat_ocupacion_ocupacion_id",
                table: "workers");

            migrationBuilder.DropTable(
                name: "ss_agente_riesgo");

            migrationBuilder.DropIndex(
                name: "ix_workers_ocupacion_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_trabajo_agente_riesgo_id",
                table: "ss_accidente_trabajo");

            migrationBuilder.DropColumn(
                name: "ocupacion_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "agente_riesgo_id",
                table: "ss_accidente_trabajo");
        }
    }
}
