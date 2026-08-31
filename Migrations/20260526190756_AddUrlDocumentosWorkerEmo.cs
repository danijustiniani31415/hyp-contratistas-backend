using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlDocumentosWorkerEmo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url_aptitud",
                table: "worker_emos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "url_emo_completo",
                table: "worker_emos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "url_aptitud",
                table: "worker_emos");

            migrationBuilder.DropColumn(
                name: "url_emo_completo",
                table: "worker_emos");
        }
    }
}
