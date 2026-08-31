using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEvAsignacionSupervisorAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ev_asignacion_supervisor ADD COLUMN IF NOT EXISTS updated_at timestamptz");
            migrationBuilder.Sql("ALTER TABLE ev_asignacion_supervisor ADD COLUMN IF NOT EXISTS updated_by_user_id integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "ev_asignacion_supervisor");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                table: "ev_asignacion_supervisor");
        }
    }
}
