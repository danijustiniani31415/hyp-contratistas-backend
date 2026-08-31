using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    public partial class AddInduccionEquipoElectrico : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columna equipo_electrico agregada manualmente en BD:
            // ALTER TABLE ss_induccion ADD COLUMN equipo_electrico boolean NOT NULL DEFAULT false;
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
