using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SplitIncluidoManualRatioYPrecio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "incluido_manual",
                table: "ss_ratio_proyecto",
                newName: "incluido_manual_ratio");

            migrationBuilder.AddColumn<bool>(
                name: "incluido_manual_precio",
                table: "ss_ratio_proyecto",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "incluido_manual_precio",
                table: "ss_ratio_proyecto");

            migrationBuilder.RenameColumn(
                name: "incluido_manual_ratio",
                table: "ss_ratio_proyecto",
                newName: "incluido_manual");
        }
    }
}
