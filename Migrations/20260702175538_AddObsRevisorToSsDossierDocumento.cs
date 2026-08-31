using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddObsRevisorToSsDossierDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_ev_evaluacion_contratista_detalle_evaluacion_id",
                table: "ev_evaluacion_contratista_detalle",
                newName: "ix_ev_evaluacion_contratista_detalle_evaluacion_contratista_id");

            migrationBuilder.AddColumn<string>(
                name: "obs_revisor",
                table: "ss_dossier_documento",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ss_charla_contratista",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    empresa_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    evidencia_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    evidencia_nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    subido_por_user_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_charla_contratista", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ss_charla_contratista");

            migrationBuilder.DropColumn(
                name: "obs_revisor",
                table: "ss_dossier_documento");

            migrationBuilder.RenameIndex(
                name: "ix_ev_evaluacion_contratista_detalle_evaluacion_contratista_id",
                table: "ev_evaluacion_contratista_detalle",
                newName: "ix_ev_evaluacion_contratista_detalle_evaluacion_id");
        }
    }
}
