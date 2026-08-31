using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Abril_Backend.Infrastructure.Data;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260806170000_MakeFechaInicioNullableEnBimProyectoFase")]
    public partial class MakeFechaInicioNullableEnBimProyectoFase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<System.DateOnly>(
                name: "fecha_inicio",
                table: "bim_proyecto_fase",
                type: "date",
                nullable: true,
                oldClrType: typeof(System.DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<System.DateOnly>(
                name: "fecha_inicio",
                table: "bim_proyecto_fase",
                type: "date",
                nullable: false,
                defaultValue: new System.DateOnly(1, 1, 1),
                oldClrType: typeof(System.DateOnly?),
                oldType: "date",
                oldNullable: true);
        }
    }
}
