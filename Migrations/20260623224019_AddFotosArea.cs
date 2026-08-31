using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFotosArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_foto_area",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inspeccion_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_foto_area", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_foto_area_ssoma_inspeccion_inspeccion_id",
                        column: x => x.inspeccion_id,
                        principalTable: "ssoma_inspeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt_foto_area",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opt_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt_foto_area", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_foto_area_ssoma_opt_opt_id",
                        column: x => x.opt_id,
                        principalTable: "ssoma_opt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_foto_area_inspeccion_id",
                table: "ssoma_inspeccion_foto_area",
                column: "inspeccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_foto_area_opt_id",
                table: "ssoma_opt_foto_area",
                column: "opt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_foto_area");

            migrationBuilder.DropTable(
                name: "ssoma_opt_foto_area");
        }
    }
}
