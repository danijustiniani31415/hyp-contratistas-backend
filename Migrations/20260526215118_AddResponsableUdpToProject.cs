using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsableUdpToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "responsable_udp",
                table: "project",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "responsable_udp_id",
                table: "project",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "responsable_udp",
                table: "project");

            migrationBuilder.DropColumn(
                name: "responsable_udp_id",
                table: "project");
        }
    }
}
