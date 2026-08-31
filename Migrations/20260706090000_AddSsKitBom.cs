using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSsKitBom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ss_kit",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    tipo_id = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_kit", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_kit_ss_material_tipo_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "ss_material_tipo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ss_kit_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kit_id = table.Column<int>(type: "integer", nullable: false),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad_por_kit = table.Column<decimal>(type: "numeric", nullable: false),
                    es_consumible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_kit_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_kit_item_ss_kit_kit_id",
                        column: x => x.kit_id,
                        principalTable: "ss_kit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_kit_item_ss_material_familia_familia_id",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_kit_tipo_id",
                table: "ss_kit",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_kit_item_kit_id",
                table: "ss_kit_item",
                column: "kit_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_kit_item_familia_id",
                table: "ss_kit_item",
                column: "familia_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ss_kit_item");
            migrationBuilder.DropTable(name: "ss_kit");
        }
    }
}
