using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorUserCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Drop FKs if they exist
                ALTER TABLE ss_sctr_vidaley DROP CONSTRAINT IF EXISTS ""fk_ss_sctr_vidaley_project_proyecto_id"";
                ALTER TABLE ss_sctr_vidaley_worker DROP CONSTRAINT IF EXISTS ""fk_ss_sctr_vidaley_worker_ss_sctr_vidaley_sctr_vida_ley_id"";

                -- Rename column/index if old name still exists
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_sctr_vidaley_worker' AND column_name='sctr_vida_ley_id') THEN
                        ALTER TABLE ss_sctr_vidaley_worker RENAME COLUMN sctr_vida_ley_id TO sctr_vidaley_id;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE tablename='ss_sctr_vidaley_worker' AND indexname='ix_ss_sctr_vidaley_worker_sctr_vida_ley_id') THEN
                        ALTER INDEX ix_ss_sctr_vidaley_worker_sctr_vida_ley_id RENAME TO ix_ss_sctr_vidaley_worker_sctr_vidaley_id;
                    END IF;
                END $$;

                -- Add columns IF NOT EXISTS
                ALTER TABLE ss_sctr_vidaley_worker ADD COLUMN IF NOT EXISTS fecha_inicio_cobertura timestamp with time zone;
                ALTER TABLE ss_sctr_vidaley ALTER COLUMN proyecto_id DROP NOT NULL;
                ALTER TABLE ss_sctr_vidaley ADD COLUMN IF NOT EXISTS fecha_inicio timestamp with time zone;
                ALTER TABLE ss_sctr_vidaley ADD COLUMN IF NOT EXISTS tipo_poliza text NOT NULL DEFAULT '';
                ALTER TABLE ss_induccion ADD COLUMN IF NOT EXISTS equipo_electrico boolean NOT NULL DEFAULT false;
                ALTER TABLE project_sub_contractor_summary_sheet ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_service_order ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_schedule ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_scanned_doc ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_quotation_file ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_promissory_note ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_contract ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_comparative_file ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_budget ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor_attached_quotation ADD COLUMN IF NOT EXISTS sharepoint_item_id text;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS project_sub_contractor_package_id integer;
                ALTER TABLE contributor ADD COLUMN IF NOT EXISTS es_abril boolean NOT NULL DEFAULT false;

                -- Create table IF NOT EXISTS
                CREATE TABLE IF NOT EXISTS project_sub_contractor_package (
                    project_sub_contractor_package_id serial PRIMARY KEY,
                    file_url text,
                    original_file_name text,
                    sharepoint_item_id text,
                    created_datetime timestamp with time zone NOT NULL,
                    created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone,
                    updated_user_id integer,
                    active boolean NOT NULL,
                    state boolean NOT NULL
                );

                -- Create index IF NOT EXISTS
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_project_sub_contractor_package_id
                    ON project_sub_contractor(project_sub_contractor_package_id);
                CREATE INDEX IF NOT EXISTS ix_ss_sctr_vidaley_worker_sctr_vidaley_id
                    ON ss_sctr_vidaley_worker(sctr_vidaley_id);

                -- Add FKs IF NOT EXISTS
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_project_sub_contractor_project_sub_contractor_package_proje') THEN
                        ALTER TABLE project_sub_contractor ADD CONSTRAINT fk_project_sub_contractor_project_sub_contractor_package_proje
                            FOREIGN KEY (project_sub_contractor_package_id) REFERENCES project_sub_contractor_package(project_sub_contractor_package_id);
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_sctr_vidaley_project_proyecto_id') THEN
                        ALTER TABLE ss_sctr_vidaley ADD CONSTRAINT fk_ss_sctr_vidaley_project_proyecto_id
                            FOREIGN KEY (proyecto_id) REFERENCES project(project_id);
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_sctr_vidaley_worker_ss_sctr_vidaley_sctr_vidaley_id') THEN
                        ALTER TABLE ss_sctr_vidaley_worker ADD CONSTRAINT fk_ss_sctr_vidaley_worker_ss_sctr_vidaley_sctr_vidaley_id
                            FOREIGN KEY (sctr_vidaley_id) REFERENCES ss_sctr_vidaley(id) ON DELETE CASCADE;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_package_proje",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_sctr_vidaley_project_proyecto_id",
                table: "ss_sctr_vidaley");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_sctr_vidaley_worker_ss_sctr_vidaley_sctr_vidaley_id",
                table: "ss_sctr_vidaley_worker");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_package");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_package_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "fecha_inicio_cobertura",
                table: "ss_sctr_vidaley_worker");

            migrationBuilder.DropColumn(
                name: "fecha_inicio",
                table: "ss_sctr_vidaley");

            migrationBuilder.DropColumn(
                name: "tipo_poliza",
                table: "ss_sctr_vidaley");

            migrationBuilder.DropColumn(
                name: "equipo_electrico",
                table: "ss_induccion");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_summary_sheet");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_service_order");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_schedule");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_scanned_doc");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_quotation_file");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_promissory_note");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_contract");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_comparative_file");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_budget");

            migrationBuilder.DropColumn(
                name: "sharepoint_item_id",
                table: "project_sub_contractor_attached_quotation");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_package_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "es_abril",
                table: "contributor");

            migrationBuilder.RenameColumn(
                name: "sctr_vidaley_id",
                table: "ss_sctr_vidaley_worker",
                newName: "sctr_vida_ley_id");

            migrationBuilder.RenameIndex(
                name: "ix_ss_sctr_vidaley_worker_sctr_vidaley_id",
                table: "ss_sctr_vidaley_worker",
                newName: "ix_ss_sctr_vidaley_worker_sctr_vida_ley_id");

            migrationBuilder.AlterColumn<int>(
                name: "proyecto_id",
                table: "ss_sctr_vidaley",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_sctr_vidaley_project_proyecto_id",
                table: "ss_sctr_vidaley",
                column: "proyecto_id",
                principalTable: "project",
                principalColumn: "project_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_sctr_vidaley_worker_ss_sctr_vidaley_sctr_vida_ley_id",
                table: "ss_sctr_vidaley_worker",
                column: "sctr_vida_ley_id",
                principalTable: "ss_sctr_vidaley",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
