using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixAccidentesIncidentesSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ss_accidente_incidente",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    responsable_id = table.Column<int>(type: "integer", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_accidente_incidente", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_accidente_incidente_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_accidente_documento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accidente_id = table.Column<int>(type: "integer", nullable: false),
                    nombre_archivo = table.Column<string>(type: "text", nullable: false),
                    tipo_archivo = table.Column<string>(type: "text", nullable: false),
                    tamanio_bytes = table.Column<long>(type: "bigint", nullable: false),
                    url_sharepoint = table.Column<string>(type: "text", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_accidente_documento", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_accidente_documento_ss_accidente_incidente_accidente_id",
                        column: x => x.accidente_id,
                        principalTable: "ss_accidente_incidente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_accidente_documento_accidente_id",
                table: "ss_accidente_documento",
                column: "accidente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_accidente_incidente_proyecto_id",
                table: "ss_accidente_incidente",
                column: "proyecto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ss_accidente_documento");

            migrationBuilder.DropTable(
                name: "ss_accidente_incidente");
        }
    }
}
