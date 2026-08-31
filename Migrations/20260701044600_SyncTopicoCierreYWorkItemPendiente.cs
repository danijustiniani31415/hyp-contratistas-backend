using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class SyncTopicoCierreYWorkItemPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "work_specialty_id",
                table: "work_item_category",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "work_item_category_id",
                table: "work_item",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cerrado_por_id",
                table: "ss_topico_atencion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "ss_topico_atencion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_cierre",
                table: "ss_topico_atencion",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "work_specialty_id",
                table: "work_item_category");

            migrationBuilder.DropColumn(
                name: "work_item_category_id",
                table: "work_item");

            migrationBuilder.DropColumn(
                name: "cerrado_por_id",
                table: "ss_topico_atencion");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "ss_topico_atencion");

            migrationBuilder.DropColumn(
                name: "fecha_cierre",
                table: "ss_topico_atencion");
        }
    }
}
