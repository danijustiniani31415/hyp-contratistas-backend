using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFactorConversionYCantidadReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "factor_conversion",
                table: "ss_material_alias",
                type: "numeric",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "cantidad_real",
                table: "ss_consumo_linea",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "precio_unitario_real",
                table: "ss_consumo_linea",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "factor_conversion",
                table: "ss_material_alias");

            migrationBuilder.DropColumn(
                name: "cantidad_real",
                table: "ss_consumo_linea");

            migrationBuilder.DropColumn(
                name: "precio_unitario_real",
                table: "ss_consumo_linea");
        }
    }
}
