using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectActivityHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_interconsultas_worker_emos_emo_id",
                table: "ss_interconsultas");

            migrationBuilder.DropColumn(
                name: "email_corporativo",
                table: "workers");

            migrationBuilder.AddColumn<bool>(
                name: "notificado",
                table: "ss_programacion_emos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "emo_id",
                table: "ss_interconsultas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "guarantee_validity_days",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_form_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_sub_contractor_anexo_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_sub_contractor_ficha_tecnica_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hierarchy_level",
                table: "project_activity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "project_activity",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_form",
                columns: table => new
                {
                    payment_form_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_form_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_form", x => x.payment_form_id);
                });

            migrationBuilder.CreateTable(
                name: "project_sub_contractor_anexo",
                columns: table => new
                {
                    project_sub_contractor_anexo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    sharepoint_item_id = table.Column<string>(type: "text", nullable: true),
                    project_sub_contractor_file_status_id = table.Column<int>(type: "integer", nullable: true),
                    observation = table.Column<string>(type: "text", nullable: true),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_sub_contractor_anexo", x => x.project_sub_contractor_anexo_id);
                    table.ForeignKey(
                        name: "fk_project_sub_contractor_anexo_project_sub_contractor_file_st",
                        column: x => x.project_sub_contractor_file_status_id,
                        principalTable: "project_sub_contractor_file_status",
                        principalColumn: "project_sub_contractor_file_status_id");
                });

            migrationBuilder.CreateTable(
                name: "project_sub_contractor_ficha_tecnica",
                columns: table => new
                {
                    project_sub_contractor_ficha_tecnica_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    sharepoint_item_id = table.Column<string>(type: "text", nullable: true),
                    project_sub_contractor_file_status_id = table.Column<int>(type: "integer", nullable: true),
                    observation = table.Column<string>(type: "text", nullable: true),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_sub_contractor_ficha_tecnica", x => x.project_sub_contractor_ficha_tecnica_id);
                    table.ForeignKey(
                        name: "fk_project_sub_contractor_ficha_tecnica_project_sub_contractor",
                        column: x => x.project_sub_contractor_file_status_id,
                        principalTable: "project_sub_contractor_file_status",
                        principalColumn: "project_sub_contractor_file_status_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_anexo_id",
                table: "project_sub_contractor",
                column: "project_sub_contractor_anexo_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_ficha_tecnica",
                table: "project_sub_contractor",
                column: "project_sub_contractor_ficha_tecnica_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_activity_parent_id",
                table: "project_activity",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_anexo_project_sub_contractor_file_st",
                table: "project_sub_contractor_anexo",
                column: "project_sub_contractor_file_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_ficha_tecnica_project_sub_contractor",
                table: "project_sub_contractor_ficha_tecnica",
                column: "project_sub_contractor_file_status_id");

            migrationBuilder.AddForeignKey(
                name: "fk_project_activity_project_activity_parent_id",
                table: "project_activity",
                column: "parent_id",
                principalTable: "project_activity",
                principalColumn: "project_activity_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_anexo_project",
                table: "project_sub_contractor",
                column: "project_sub_contractor_anexo_id",
                principalTable: "project_sub_contractor_anexo",
                principalColumn: "project_sub_contractor_anexo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_ficha_tecnica",
                table: "project_sub_contractor",
                column: "project_sub_contractor_ficha_tecnica_id",
                principalTable: "project_sub_contractor_ficha_tecnica",
                principalColumn: "project_sub_contractor_ficha_tecnica_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_interconsultas_worker_emos_emo_id",
                table: "ss_interconsultas",
                column: "emo_id",
                principalTable: "worker_emos",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_project_activity_project_activity_parent_id",
                table: "project_activity");

            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_anexo_project",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_ficha_tecnica",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_interconsultas_worker_emos_emo_id",
                table: "ss_interconsultas");

            migrationBuilder.DropTable(
                name: "payment_form");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_anexo");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_ficha_tecnica");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_anexo_id",
                table: "project_sub_contractor");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_ficha_tecnica",
                table: "project_sub_contractor");

            migrationBuilder.DropIndex(
                name: "ix_project_activity_parent_id",
                table: "project_activity");

            migrationBuilder.DropColumn(
                name: "notificado",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "guarantee_validity_days",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "payment_form_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_anexo_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_ficha_tecnica_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "hierarchy_level",
                table: "project_activity");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "project_activity");

            migrationBuilder.AddColumn<string>(
                name: "email_corporativo",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "emo_id",
                table: "ss_interconsultas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_interconsultas_worker_emos_emo_id",
                table: "ss_interconsultas",
                column: "emo_id",
                principalTable: "worker_emos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
