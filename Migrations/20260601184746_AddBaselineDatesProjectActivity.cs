using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBaselineDatesProjectActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "baseline_end_date",
                table: "project_activity",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "baseline_start_date",
                table: "project_activity",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "baseline_end_date",
                table: "project_activity");

            migrationBuilder.DropColumn(
                name: "baseline_start_date",
                table: "project_activity");
        }
    }
}
