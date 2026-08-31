using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncSnapshot2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migración vacía — columnas ya añadidas por AddUrlDocumentosWorkerEmo. Solo sincroniza el snapshot de EF.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
