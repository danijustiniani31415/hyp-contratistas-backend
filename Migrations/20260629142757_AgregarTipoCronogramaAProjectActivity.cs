using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoCronogramaAProjectActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aprobador_email",
                table: "ga_solicitud_salida");

            migrationBuilder.AddColumn<string>(
                name: "tipo_cronograma",
                table: "project_activity",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ANTEPROYECTO");

            migrationBuilder.AddColumn<int>(
                name: "aprobador_worker_id",
                table: "ga_solicitud_salida",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vecino_licencia",
                columns: table => new
                {
                    vecino_licencia_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    archivo_url = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_recordatorio = table.Column<DateOnly>(type: "date", nullable: false),
                    dias_antes = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_licencia", x => x.vecino_licencia_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vecino_licencia");

            migrationBuilder.DropColumn(
                name: "tipo_cronograma",
                table: "project_activity");

            migrationBuilder.DropColumn(
                name: "aprobador_worker_id",
                table: "ga_solicitud_salida");

            migrationBuilder.AddColumn<string>(
                name: "aprobador_email",
                table: "ga_solicitud_salida",
                type: "text",
                nullable: true);
        }
    }
}
