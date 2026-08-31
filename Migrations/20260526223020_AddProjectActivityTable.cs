using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectActivityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_activity",
                columns: table => new
                {
                    project_activity_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    activity_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    planned_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    planned_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    progress_percentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    project_activity_order = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_activity", x => x.project_activity_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_activity");
        }
    }
}
