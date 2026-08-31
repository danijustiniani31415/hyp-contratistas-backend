using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSexoPersonaYCie10Accidente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "diagnostico_cie10",
                table: "ss_accidente_trabajo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sexo",
                table: "person",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "diagnostico_cie10",
                table: "ss_accidente_trabajo");

            migrationBuilder.DropColumn(
                name: "sexo",
                table: "person");
        }
    }
}
