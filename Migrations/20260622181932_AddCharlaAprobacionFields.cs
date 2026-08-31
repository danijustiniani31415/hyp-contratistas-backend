using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCharlaAprobacionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "aprobado_en",
                table: "ss_charla",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "aprobado_por_id",
                table: "ss_charla",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "ss_charla",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aprobado_en",
                table: "ss_charla");

            migrationBuilder.DropColumn(
                name: "aprobado_por_id",
                table: "ss_charla");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "ss_charla");
        }
    }
}
