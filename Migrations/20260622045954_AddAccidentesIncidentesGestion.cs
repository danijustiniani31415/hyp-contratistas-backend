using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccidentesIncidentesGestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo_atencion",
                table: "ss_topico_atencion");

            migrationBuilder.AddColumn<int>(
                name: "tipo_atencion_id",
                table: "ss_topico_atencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_ss_topico_atencion_tipo_atencion_id",
                table: "ss_topico_atencion",
                column: "tipo_atencion_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_topico_atencion_ss_topico_tipo_atencion_tipo_atencion_id",
                table: "ss_topico_atencion",
                column: "tipo_atencion_id",
                principalTable: "ss_topico_tipo_atencion",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_topico_atencion_ss_topico_tipo_atencion_tipo_atencion_id",
                table: "ss_topico_atencion");

            migrationBuilder.DropIndex(
                name: "ix_ss_topico_atencion_tipo_atencion_id",
                table: "ss_topico_atencion");

            migrationBuilder.DropColumn(
                name: "tipo_atencion_id",
                table: "ss_topico_atencion");

            migrationBuilder.AddColumn<string>(
                name: "tipo_atencion",
                table: "ss_topico_atencion",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
