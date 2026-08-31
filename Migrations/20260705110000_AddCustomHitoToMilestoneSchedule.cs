using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomHitoToMilestoneSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "milestone_id",
                table: "milestone_schedule",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "custom_description",
                table: "milestone_schedule",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_description",
                table: "milestone_schedule");

            migrationBuilder.AlterColumn<int>(
                name: "milestone_id",
                table: "milestone_schedule",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int?),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
