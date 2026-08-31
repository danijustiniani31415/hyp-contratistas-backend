using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    public partial class AddEsRecaidaAndAplicaA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Campo es_recaida en ss_descanso_medico
            migrationBuilder.AddColumn<bool>(
                name: "es_recaida",
                table: "ss_descanso_medico",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Campo aplica_a en ss_entregable_tipo
            migrationBuilder.AddColumn<string>(
                name: "aplica_a",
                table: "ss_entregable_tipo",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TODOS");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_recaida",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "aplica_a",
                table: "ss_entregable_tipo");
        }
    }
}
