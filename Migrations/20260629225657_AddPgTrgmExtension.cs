using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPgTrgmExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_ss_material_familia_nombre_normalizado_trgm " +
                "ON ss_material_familia USING gin (nombre_normalizado gin_trgm_ops);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_ss_material_item_nombre_normalizado_trgm " +
                "ON ss_material_item USING gin (nombre_normalizado gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ss_material_item_nombre_normalizado_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ss_material_familia_nombre_normalizado_trgm;");
        }
    }
}
