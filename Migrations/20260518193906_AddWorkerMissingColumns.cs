using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Drop constraints IF EXISTS
                ALTER TABLE ss_alertas_emo DROP CONSTRAINT IF EXISTS ""fk_ss_alertas_emo_worker_emos_emo_id"";
                ALTER TABLE ss_alertas_emo DROP CONSTRAINT IF EXISTS ""fk_ss_alertas_emo_workers_worker_id"";

                -- Drop columns IF EXISTS
                ALTER TABLE workers DROP COLUMN IF EXISTS apellido_nombre;
                ALTER TABLE workers DROP COLUMN IF EXISTS dni;
                ALTER TABLE workers DROP COLUMN IF EXISTS ruc;
                ALTER TABLE project_sub_contractor DROP COLUMN IF EXISTS contract_id;

                -- Add columns IF NOT EXISTS
                ALTER TABLE workers ADD COLUMN IF NOT EXISTS contributor_id integer;
                ALTER TABLE workers ADD COLUMN IF NOT EXISTS person_id integer;
                ALTER TABLE work_item_category ADD COLUMN IF NOT EXISTS instructivos_folder_id text;
                ALTER TABLE work_item_category ADD COLUMN IF NOT EXISTS instructivos_folder_name text;
                ALTER TABLE work_item_category ADD COLUMN IF NOT EXISTS instructivos_sync_status integer;
                ALTER TABLE work_item_category ADD COLUMN IF NOT EXISTS instructivos_synced_at timestamp with time zone;
                ALTER TABLE ss_programacion_emos ADD COLUMN IF NOT EXISTS check_in_hora time without time zone;
                ALTER TABLE ss_programacion_emos ADD COLUMN IF NOT EXISTS fecha_notificacion timestamp with time zone;
                ALTER TABLE ss_programacion_emos ADD COLUMN IF NOT EXISTS motivo_rechazo text;
                ALTER TABLE ss_programacion_emos ADD COLUMN IF NOT EXISTS origen text NOT NULL DEFAULT 'Manual';
                ALTER TABLE ss_clinicas ADD COLUMN IF NOT EXISTS password_hash text NOT NULL DEFAULT '';
                ALTER TABLE ss_alertas_emo ALTER COLUMN worker_id DROP NOT NULL;
                ALTER TABLE ss_alertas_emo ALTER COLUMN emo_id DROP NOT NULL;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS contract_modality_id integer;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS guarantee_fund_days integer;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS guarantee_fund_percentage integer;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS project_sub_contractor_instructivo_id integer;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS project_sub_contractor_non_conforming_output_id integer;
                ALTER TABLE project_sub_contractor ADD COLUMN IF NOT EXISTS project_sub_contractor_tolerance_chart_id integer;
                ALTER TABLE project ADD COLUMN IF NOT EXISTS abbreviation text;
                ALTER TABLE phase_stage_sub_stage_sub_specialty ADD COLUMN IF NOT EXISTS partida_id integer;
                ALTER TABLE lesson ADD COLUMN IF NOT EXISTS sub_area_id integer;
                ALTER TABLE contractor_email ADD COLUMN IF NOT EXISTS contractor_person_type_id integer;
                ALTER TABLE contractor ADD COLUMN IF NOT EXISTS activation_token text;
                ALTER TABLE contractor ADD COLUMN IF NOT EXISTS activation_token_expiry timestamp with time zone;
                ALTER TABLE contractor ADD COLUMN IF NOT EXISTS logo_file_url text;
                ALTER TABLE ac_actividades ADD COLUMN IF NOT EXISTS categoria_id integer;
                ALTER TABLE ac_actividades ADD COLUMN IF NOT EXISTS especialidad_id integer;

                -- Create tables IF NOT EXISTS
                CREATE TABLE IF NOT EXISTS cat_jefatura (
                    id serial PRIMARY KEY, nombre text NOT NULL, email text, activo boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS contract_modality (
                    contract_modality_id serial PRIMARY KEY, contract_modality_description text NOT NULL,
                    state boolean NOT NULL, created_datetime timestamp with time zone NOT NULL,
                    updated_datetime timestamp with time zone);
                CREATE TABLE IF NOT EXISTS contractor_person_type (
                    contractor_person_type_id serial PRIMARY KEY, description text NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS contractor_user (
                    contractor_user_id serial PRIMARY KEY, contractor_id integer NOT NULL, user_id integer NOT NULL,
                    created_date_time timestamp with time zone NOT NULL, created_user_id integer,
                    active boolean NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS ga_hora_opcion (
                    id serial PRIMARY KEY, hora time without time zone NOT NULL, etiqueta text NOT NULL, activo boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS ga_lugar (
                    id serial PRIMARY KEY, tipo text NOT NULL, nombre text, project_id integer,
                    activo boolean NOT NULL, orden integer NOT NULL, created_at timestamp with time zone NOT NULL);
                CREATE TABLE IF NOT EXISTS ga_motivo_salida (
                    id serial PRIMARY KEY, descripcion text NOT NULL, activo boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL);
                CREATE TABLE IF NOT EXISTS ga_solicitud_salida (
                    id serial PRIMARY KEY, worker_id integer NOT NULL, fecha_salida date NOT NULL,
                    hora_salida time without time zone NOT NULL, hora_retorno time without time zone,
                    motivo_id integer NOT NULL, lugar_origen_id integer, lugar_origen_libre text,
                    lugar_destino_id integer, lugar_destino_libre text, estado text NOT NULL,
                    registrado_por_id integer, created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL);
                CREATE TABLE IF NOT EXISTS partida (
                    partida_id serial PRIMARY KEY, partida_description text NOT NULL, state boolean NOT NULL,
                    active boolean NOT NULL, created_date_time timestamp with time zone NOT NULL,
                    created_user_id integer, updated_date_time timestamp with time zone, updated_user_id integer);
                CREATE TABLE IF NOT EXISTS project_link (
                    project_link_id serial PRIMARY KEY, project_id integer NOT NULL,
                    project_link_type_id integer NOT NULL, link_url text NOT NULL,
                    created_date_time timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_date_time timestamp with time zone, updated_user_id integer,
                    active boolean NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS project_link_type (
                    project_link_type_id serial PRIMARY KEY, project_link_type_description text NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS project_sub_contractor_instructivo (
                    project_sub_contractor_instructivo_id serial PRIMARY KEY, file_url text, original_file_name text,
                    sharepoint_item_id text, project_sub_contractor_file_status_id integer, observation text,
                    created_datetime timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone, updated_user_id integer,
                    active boolean NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS project_sub_contractor_non_conforming_output (
                    project_sub_contractor_non_conforming_output_id serial PRIMARY KEY, file_url text, original_file_name text,
                    sharepoint_item_id text, project_sub_contractor_file_status_id integer, observation text,
                    created_datetime timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone, updated_user_id integer,
                    active boolean NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS project_sub_contractor_tolerance_chart (
                    project_sub_contractor_tolerance_chart_id serial PRIMARY KEY, file_url text, original_file_name text,
                    sharepoint_item_id text, project_sub_contractor_file_status_id integer, observation text,
                    created_datetime timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone, updated_user_id integer,
                    active boolean NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS psss_scope (
                    psss_scope_id serial PRIMARY KEY, phase_stage_sub_stage_sub_specialty_id integer NOT NULL,
                    area_id integer, sub_area_id integer, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS psss_template (
                    psss_template_id serial PRIMARY KEY, template_name text NOT NULL, description text,
                    state boolean NOT NULL, active boolean NOT NULL,
                    created_date_time timestamp with time zone NOT NULL, created_user_id integer,
                    updated_date_time timestamp with time zone, updated_user_id integer);
                CREATE TABLE IF NOT EXISTS psss_template_detail (
                    psss_template_detail_id serial PRIMARY KEY, psss_template_id integer NOT NULL,
                    phase_stage_sub_stage_sub_specialty_id integer NOT NULL, state boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS ss_clinica_emails (
                    id serial PRIMARY KEY, clinica_id integer NOT NULL REFERENCES ss_clinicas(id) ON DELETE CASCADE,
                    nombre text, email text NOT NULL, activo boolean NOT NULL);
                CREATE TABLE IF NOT EXISTS ss_clinica_reset_token (
                    id serial PRIMARY KEY, clinica_id integer NOT NULL REFERENCES ss_clinicas(id) ON DELETE CASCADE,
                    token text NOT NULL, expira_en timestamp with time zone NOT NULL,
                    usado boolean NOT NULL, created_at timestamp with time zone NOT NULL);
                CREATE TABLE IF NOT EXISTS ss_clinica_usuarios (
                    clinica_usuario_id serial PRIMARY KEY, clinica_id integer NOT NULL REFERENCES ss_clinicas(id) ON DELETE CASCADE,
                    nombre text NOT NULL, email text NOT NULL, password_hash text NOT NULL, activo boolean NOT NULL,
                    ultimo_acceso timestamp with time zone, creado_en timestamp with time zone NOT NULL, creado_por integer,
                    modificado_en timestamp with time zone, modificado_por integer,
                    desactivado_en timestamp with time zone, desactivado_por integer);
                CREATE TABLE IF NOT EXISTS ss_clinica_auditoria (
                    auditoria_id serial PRIMARY KEY, clinica_usuario_id integer REFERENCES ss_clinica_usuarios(clinica_usuario_id),
                    clinica_id integer, accion text NOT NULL, realizado_en timestamp with time zone NOT NULL,
                    ip_origen text, user_agent text, detalle_adicional jsonb);
                CREATE TABLE IF NOT EXISTS ss_clinica_tokens (
                    token_id serial PRIMARY KEY, clinica_usuario_id integer NOT NULL REFERENCES ss_clinica_usuarios(clinica_usuario_id) ON DELETE CASCADE,
                    token text NOT NULL, tipo text NOT NULL, expiracion timestamp with time zone NOT NULL,
                    usado_en timestamp with time zone, creado_en timestamp with time zone NOT NULL, ip_solicitud text);
                CREATE TABLE IF NOT EXISTS sub_area (
                    sub_area_id serial PRIMARY KEY, area_id integer NOT NULL REFERENCES area(area_id) ON DELETE CASCADE,
                    sub_area_description text NOT NULL, active boolean NOT NULL, state boolean NOT NULL,
                    created_date_time timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_date_time timestamp with time zone, updated_user_id integer);
                CREATE TABLE IF NOT EXISTS work_item_category_clause (
                    work_item_category_clause_id serial PRIMARY KEY,
                    work_item_category_id integer NOT NULL REFERENCES work_item_category(work_item_category_id) ON DELETE CASCADE,
                    clause_text text NOT NULL, sort_order integer NOT NULL, state boolean NOT NULL,
                    created_datetime timestamp with time zone NOT NULL, created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone, updated_user_id integer);

                -- Create indexes IF NOT EXISTS
                CREATE INDEX IF NOT EXISTS ix_workers_contributor_id ON workers(contributor_id);
                CREATE INDEX IF NOT EXISTS ix_workers_person_id ON workers(person_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_project_sub_contractor_instructivo_id ON project_sub_contractor(project_sub_contractor_instructivo_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_project_sub_contractor_non_conformin ON project_sub_contractor(project_sub_contractor_non_conforming_output_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_project_sub_contractor_tolerance_cha ON project_sub_contractor(project_sub_contractor_tolerance_chart_id);
                CREATE INDEX IF NOT EXISTS ix_contractor_email_contractor_person_type_id ON contractor_email(contractor_person_type_id);
                CREATE INDEX IF NOT EXISTS ix_contractor_user_contractor_id ON contractor_user(contractor_id);
                CREATE INDEX IF NOT EXISTS ix_contractor_user_user_id ON contractor_user(user_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_instructivo_project_sub_contractor_f ON project_sub_contractor_instructivo(project_sub_contractor_file_status_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_non_conforming_output_project_sub_co ON project_sub_contractor_non_conforming_output(project_sub_contractor_file_status_id);
                CREATE INDEX IF NOT EXISTS ix_project_sub_contractor_tolerance_chart_project_sub_contract ON project_sub_contractor_tolerance_chart(project_sub_contractor_file_status_id);
                CREATE INDEX IF NOT EXISTS ix_ss_clinica_auditoria_clinica_usuario_id ON ss_clinica_auditoria(clinica_usuario_id);
                CREATE INDEX IF NOT EXISTS ix_ss_clinica_emails_clinica_id ON ss_clinica_emails(clinica_id);
                CREATE INDEX IF NOT EXISTS ix_ss_clinica_reset_token_clinica_id ON ss_clinica_reset_token(clinica_id);
                CREATE INDEX IF NOT EXISTS ix_ss_clinica_tokens_clinica_usuario_id ON ss_clinica_tokens(clinica_usuario_id);
                CREATE INDEX IF NOT EXISTS ix_ss_clinica_usuarios_clinica_id ON ss_clinica_usuarios(clinica_id);
                CREATE INDEX IF NOT EXISTS ix_sub_area_area_id ON sub_area(area_id);
                CREATE INDEX IF NOT EXISTS ix_work_item_category_clause_work_item_category_id ON work_item_category_clause(work_item_category_id);

                -- Add FKs IF NOT EXISTS
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_contractor_email_contractor_person_type_contractor_person_t') THEN
                    ALTER TABLE contractor_email ADD CONSTRAINT fk_contractor_email_contractor_person_type_contractor_person_t FOREIGN KEY (contractor_person_type_id) REFERENCES contractor_person_type(contractor_person_type_id) ON DELETE RESTRICT; END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_project_sub_contractor_project_sub_contractor_instructivo_p') THEN
                    ALTER TABLE project_sub_contractor ADD CONSTRAINT fk_project_sub_contractor_project_sub_contractor_instructivo_p FOREIGN KEY (project_sub_contractor_instructivo_id) REFERENCES project_sub_contractor_instructivo(project_sub_contractor_instructivo_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_project_sub_contractor_project_sub_contractor_non_conformin') THEN
                    ALTER TABLE project_sub_contractor ADD CONSTRAINT fk_project_sub_contractor_project_sub_contractor_non_conformin FOREIGN KEY (project_sub_contractor_non_conforming_output_id) REFERENCES project_sub_contractor_non_conforming_output(project_sub_contractor_non_conforming_output_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_project_sub_contractor_project_sub_contractor_tolerance_cha') THEN
                    ALTER TABLE project_sub_contractor ADD CONSTRAINT fk_project_sub_contractor_project_sub_contractor_tolerance_cha FOREIGN KEY (project_sub_contractor_tolerance_chart_id) REFERENCES project_sub_contractor_tolerance_chart(project_sub_contractor_tolerance_chart_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_alertas_emo_worker_emos_emo_id') THEN
                    ALTER TABLE ss_alertas_emo ADD CONSTRAINT fk_ss_alertas_emo_worker_emos_emo_id FOREIGN KEY (emo_id) REFERENCES worker_emos(id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_ss_alertas_emo_workers_worker_id') THEN
                    ALTER TABLE ss_alertas_emo ADD CONSTRAINT fk_ss_alertas_emo_workers_worker_id FOREIGN KEY (worker_id) REFERENCES workers(id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_workers_contributor_contributor_id') THEN
                    ALTER TABLE workers ADD CONSTRAINT fk_workers_contributor_contributor_id FOREIGN KEY (contributor_id) REFERENCES contributor(contributor_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_workers_person_person_id') THEN
                    ALTER TABLE workers ADD CONSTRAINT fk_workers_person_person_id FOREIGN KEY (person_id) REFERENCES person(person_id); END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_contractor_user_contractor_contractor_id') THEN
                    ALTER TABLE contractor_user ADD CONSTRAINT fk_contractor_user_contractor_contractor_id FOREIGN KEY (contractor_id) REFERENCES contractor(contractor_id) ON DELETE RESTRICT; END IF; END $$;
                DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_contractor_user_user_user_id') THEN
                    ALTER TABLE contractor_user ADD CONSTRAINT fk_contractor_user_user_user_id FOREIGN KEY (user_id) REFERENCES app_user(user_id) ON DELETE RESTRICT; END IF; END $$;
            ");
        }

        private void _unused_legacy(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "contributor_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "person_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instructivos_folder_id",
                table: "work_item_category",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instructivos_folder_name",
                table: "work_item_category",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "instructivos_sync_status",
                table: "work_item_category",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "instructivos_synced_at",
                table: "work_item_category",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "check_in_hora",
                table: "ss_programacion_emos",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_notificacion",
                table: "ss_programacion_emos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "ss_programacion_emos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origen",
                table: "ss_programacion_emos",
                type: "text",
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "ss_clinicas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "worker_id",
                table: "ss_alertas_emo",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "emo_id",
                table: "ss_alertas_emo",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "contract_modality_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "guarantee_fund_days",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "guarantee_fund_percentage",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_sub_contractor_instructivo_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_sub_contractor_non_conforming_output_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_sub_contractor_tolerance_chart_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "abbreviation",
                table: "project",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "partida_id",
                table: "phase_stage_sub_stage_sub_specialty",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sub_area_id",
                table: "lesson",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "contractor_person_type_id",
                table: "contractor_email",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activation_token",
                table: "contractor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "activation_token_expiry",
                table: "contractor",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_file_url",
                table: "contractor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "categoria_id",
                table: "ac_actividades",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "especialidad_id",
                table: "ac_actividades",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cat_jefatura",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cat_jefatura", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_modality",
                columns: table => new
                {
                    contract_modality_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_modality_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_modality", x => x.contract_modality_id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_person_type",
                columns: table => new
                {
                    contractor_person_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_person_type", x => x.contractor_person_type_id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_user",
                columns: table => new
                {
                    contractor_user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contractor_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_user", x => x.contractor_user_id);
                    table.ForeignKey(
                        name: "fk_contractor_user_contractor_contractor_id",
                        column: x => x.contractor_id,
                        principalTable: "contractor",
                        principalColumn: "contractor_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contractor_user_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ga_hora_opcion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    etiqueta = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ga_hora_opcion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ga_lugar",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ga_lugar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ga_motivo_salida",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ga_motivo_salida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ga_solicitud_salida",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_salida = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_salida = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_retorno = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    motivo_id = table.Column<int>(type: "integer", nullable: false),
                    lugar_origen_id = table.Column<int>(type: "integer", nullable: true),
                    lugar_origen_libre = table.Column<string>(type: "text", nullable: true),
                    lugar_destino_id = table.Column<int>(type: "integer", nullable: true),
                    lugar_destino_libre = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ga_solicitud_salida", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partida",
                columns: table => new
                {
                    partida_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    partida_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partida", x => x.partida_id);
                });

            migrationBuilder.CreateTable(
                name: "project_link",
                columns: table => new
                {
                    project_link_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    project_link_type_id = table.Column<int>(type: "integer", nullable: false),
                    link_url = table.Column<string>(type: "text", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_link", x => x.project_link_id);
                });

            migrationBuilder.CreateTable(
                name: "project_link_type",
                columns: table => new
                {
                    project_link_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_link_type_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_link_type", x => x.project_link_type_id);
                });

            migrationBuilder.CreateTable(
                name: "project_sub_contractor_instructivo",
                columns: table => new
                {
                    project_sub_contractor_instructivo_id = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("pk_project_sub_contractor_instructivo", x => x.project_sub_contractor_instructivo_id);
                    table.ForeignKey(
                        name: "fk_project_sub_contractor_instructivo_project_sub_contractor_f",
                        column: x => x.project_sub_contractor_file_status_id,
                        principalTable: "project_sub_contractor_file_status",
                        principalColumn: "project_sub_contractor_file_status_id");
                });

            migrationBuilder.CreateTable(
                name: "project_sub_contractor_non_conforming_output",
                columns: table => new
                {
                    project_sub_contractor_non_conforming_output_id = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("pk_project_sub_contractor_non_conforming_output", x => x.project_sub_contractor_non_conforming_output_id);
                    table.ForeignKey(
                        name: "fk_project_sub_contractor_non_conforming_output_project_sub_co",
                        column: x => x.project_sub_contractor_file_status_id,
                        principalTable: "project_sub_contractor_file_status",
                        principalColumn: "project_sub_contractor_file_status_id");
                });

            migrationBuilder.CreateTable(
                name: "project_sub_contractor_tolerance_chart",
                columns: table => new
                {
                    project_sub_contractor_tolerance_chart_id = table.Column<int>(type: "integer", nullable: false)
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
                    table.PrimaryKey("pk_project_sub_contractor_tolerance_chart", x => x.project_sub_contractor_tolerance_chart_id);
                    table.ForeignKey(
                        name: "fk_project_sub_contractor_tolerance_chart_project_sub_contract",
                        column: x => x.project_sub_contractor_file_status_id,
                        principalTable: "project_sub_contractor_file_status",
                        principalColumn: "project_sub_contractor_file_status_id");
                });

            migrationBuilder.CreateTable(
                name: "psss_scope",
                columns: table => new
                {
                    psss_scope_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    phase_stage_sub_stage_sub_specialty_id = table.Column<int>(type: "integer", nullable: false),
                    area_id = table.Column<int>(type: "integer", nullable: true),
                    sub_area_id = table.Column<int>(type: "integer", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_psss_scope", x => x.psss_scope_id);
                });

            migrationBuilder.CreateTable(
                name: "psss_template",
                columns: table => new
                {
                    psss_template_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_psss_template", x => x.psss_template_id);
                });

            migrationBuilder.CreateTable(
                name: "psss_template_detail",
                columns: table => new
                {
                    psss_template_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    psss_template_id = table.Column<int>(type: "integer", nullable: false),
                    phase_stage_sub_stage_sub_specialty_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_psss_template_detail", x => x.psss_template_detail_id);
                });

            migrationBuilder.CreateTable(
                name: "ss_clinica_emails",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clinica_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_clinica_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_clinica_emails_ss_clinicas_clinica_id",
                        column: x => x.clinica_id,
                        principalTable: "ss_clinicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_clinica_reset_token",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clinica_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expira_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_clinica_reset_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_clinica_reset_token_ss_clinicas_clinica_id",
                        column: x => x.clinica_id,
                        principalTable: "ss_clinicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_clinica_usuarios",
                columns: table => new
                {
                    clinica_usuario_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clinica_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    modificado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modificado_por = table.Column<int>(type: "integer", nullable: true),
                    desactivado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    desactivado_por = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_clinica_usuarios", x => x.clinica_usuario_id);
                    table.ForeignKey(
                        name: "fk_ss_clinica_usuarios_ss_clinicas_clinica_id",
                        column: x => x.clinica_id,
                        principalTable: "ss_clinicas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_area",
                columns: table => new
                {
                    sub_area_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    area_id = table.Column<int>(type: "integer", nullable: false),
                    sub_area_description = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_area", x => x.sub_area_id);
                    table.ForeignKey(
                        name: "fk_sub_area_area_area_id",
                        column: x => x.area_id,
                        principalTable: "area",
                        principalColumn: "area_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_item_category_clause",
                columns: table => new
                {
                    work_item_category_clause_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_item_category_id = table.Column<int>(type: "integer", nullable: false),
                    clause_text = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_category_clause", x => x.work_item_category_clause_id);
                    table.ForeignKey(
                        name: "fk_work_item_category_clause_work_item_category_work_item_cate",
                        column: x => x.work_item_category_id,
                        principalTable: "work_item_category",
                        principalColumn: "work_item_category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_clinica_auditoria",
                columns: table => new
                {
                    auditoria_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clinica_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    clinica_id = table.Column<int>(type: "integer", nullable: true),
                    accion = table.Column<string>(type: "text", nullable: false),
                    realizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_origen = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    detalle_adicional = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_clinica_auditoria", x => x.auditoria_id);
                    table.ForeignKey(
                        name: "fk_ss_clinica_auditoria_ss_clinica_usuarios_clinica_usuario_id",
                        column: x => x.clinica_usuario_id,
                        principalTable: "ss_clinica_usuarios",
                        principalColumn: "clinica_usuario_id");
                });

            migrationBuilder.CreateTable(
                name: "ss_clinica_tokens",
                columns: table => new
                {
                    token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clinica_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    expiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_solicitud = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_clinica_tokens", x => x.token_id);
                    table.ForeignKey(
                        name: "fk_ss_clinica_tokens_ss_clinica_usuarios_clinica_usuario_id",
                        column: x => x.clinica_usuario_id,
                        principalTable: "ss_clinica_usuarios",
                        principalColumn: "clinica_usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workers_contributor_id",
                table: "workers",
                column: "contributor_id");

            migrationBuilder.CreateIndex(
                name: "ix_workers_person_id",
                table: "workers",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_instructivo_id",
                table: "project_sub_contractor",
                column: "project_sub_contractor_instructivo_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_non_conformin",
                table: "project_sub_contractor",
                column: "project_sub_contractor_non_conforming_output_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_tolerance_cha",
                table: "project_sub_contractor",
                column: "project_sub_contractor_tolerance_chart_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_email_contractor_person_type_id",
                table: "contractor_email",
                column: "contractor_person_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_user_contractor_id",
                table: "contractor_user",
                column: "contractor_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_user_user_id",
                table: "contractor_user",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_instructivo_project_sub_contractor_f",
                table: "project_sub_contractor_instructivo",
                column: "project_sub_contractor_file_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_non_conforming_output_project_sub_co",
                table: "project_sub_contractor_non_conforming_output",
                column: "project_sub_contractor_file_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_sub_contractor_tolerance_chart_project_sub_contract",
                table: "project_sub_contractor_tolerance_chart",
                column: "project_sub_contractor_file_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_clinica_auditoria_clinica_usuario_id",
                table: "ss_clinica_auditoria",
                column: "clinica_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_clinica_emails_clinica_id",
                table: "ss_clinica_emails",
                column: "clinica_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_clinica_reset_token_clinica_id",
                table: "ss_clinica_reset_token",
                column: "clinica_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_clinica_tokens_clinica_usuario_id",
                table: "ss_clinica_tokens",
                column: "clinica_usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_clinica_usuarios_clinica_id",
                table: "ss_clinica_usuarios",
                column: "clinica_id");

            migrationBuilder.CreateIndex(
                name: "ix_sub_area_area_id",
                table: "sub_area",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_category_clause_work_item_category_id",
                table: "work_item_category_clause",
                column: "work_item_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contractor_email_contractor_person_type_contractor_person_t",
                table: "contractor_email",
                column: "contractor_person_type_id",
                principalTable: "contractor_person_type",
                principalColumn: "contractor_person_type_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_instructivo_p",
                table: "project_sub_contractor",
                column: "project_sub_contractor_instructivo_id",
                principalTable: "project_sub_contractor_instructivo",
                principalColumn: "project_sub_contractor_instructivo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_non_conformin",
                table: "project_sub_contractor",
                column: "project_sub_contractor_non_conforming_output_id",
                principalTable: "project_sub_contractor_non_conforming_output",
                principalColumn: "project_sub_contractor_non_conforming_output_id");

            migrationBuilder.AddForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_tolerance_cha",
                table: "project_sub_contractor",
                column: "project_sub_contractor_tolerance_chart_id",
                principalTable: "project_sub_contractor_tolerance_chart",
                principalColumn: "project_sub_contractor_tolerance_chart_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_alertas_emo_worker_emos_emo_id",
                table: "ss_alertas_emo",
                column: "emo_id",
                principalTable: "worker_emos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_alertas_emo_workers_worker_id",
                table: "ss_alertas_emo",
                column: "worker_id",
                principalTable: "workers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_contributor_contributor_id",
                table: "workers",
                column: "contributor_id",
                principalTable: "contributor",
                principalColumn: "contributor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_person_person_id",
                table: "workers",
                column: "person_id",
                principalTable: "person",
                principalColumn: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contractor_email_contractor_person_type_contractor_person_t",
                table: "contractor_email");

            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_instructivo_p",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_non_conformin",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_project_sub_contractor_project_sub_contractor_tolerance_cha",
                table: "project_sub_contractor");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_alertas_emo_worker_emos_emo_id",
                table: "ss_alertas_emo");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_alertas_emo_workers_worker_id",
                table: "ss_alertas_emo");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_contributor_contributor_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_person_person_id",
                table: "workers");

            migrationBuilder.DropTable(
                name: "cat_jefatura");

            migrationBuilder.DropTable(
                name: "contract_modality");

            migrationBuilder.DropTable(
                name: "contractor_person_type");

            migrationBuilder.DropTable(
                name: "contractor_user");

            migrationBuilder.DropTable(
                name: "ga_hora_opcion");

            migrationBuilder.DropTable(
                name: "ga_lugar");

            migrationBuilder.DropTable(
                name: "ga_motivo_salida");

            migrationBuilder.DropTable(
                name: "ga_solicitud_salida");

            migrationBuilder.DropTable(
                name: "partida");

            migrationBuilder.DropTable(
                name: "project_link");

            migrationBuilder.DropTable(
                name: "project_link_type");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_instructivo");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_non_conforming_output");

            migrationBuilder.DropTable(
                name: "project_sub_contractor_tolerance_chart");

            migrationBuilder.DropTable(
                name: "psss_scope");

            migrationBuilder.DropTable(
                name: "psss_template");

            migrationBuilder.DropTable(
                name: "psss_template_detail");

            migrationBuilder.DropTable(
                name: "ss_clinica_auditoria");

            migrationBuilder.DropTable(
                name: "ss_clinica_emails");

            migrationBuilder.DropTable(
                name: "ss_clinica_reset_token");

            migrationBuilder.DropTable(
                name: "ss_clinica_tokens");

            migrationBuilder.DropTable(
                name: "sub_area");

            migrationBuilder.DropTable(
                name: "work_item_category_clause");

            migrationBuilder.DropTable(
                name: "ss_clinica_usuarios");

            migrationBuilder.DropIndex(
                name: "ix_workers_contributor_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_workers_person_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_instructivo_id",
                table: "project_sub_contractor");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_non_conformin",
                table: "project_sub_contractor");

            migrationBuilder.DropIndex(
                name: "ix_project_sub_contractor_project_sub_contractor_tolerance_cha",
                table: "project_sub_contractor");

            migrationBuilder.DropIndex(
                name: "ix_contractor_email_contractor_person_type_id",
                table: "contractor_email");

            migrationBuilder.DropColumn(
                name: "contributor_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "person_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "instructivos_folder_id",
                table: "work_item_category");

            migrationBuilder.DropColumn(
                name: "instructivos_folder_name",
                table: "work_item_category");

            migrationBuilder.DropColumn(
                name: "instructivos_sync_status",
                table: "work_item_category");

            migrationBuilder.DropColumn(
                name: "instructivos_synced_at",
                table: "work_item_category");

            migrationBuilder.DropColumn(
                name: "check_in_hora",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "fecha_notificacion",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "origen",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "ss_clinicas");

            migrationBuilder.DropColumn(
                name: "contract_modality_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "guarantee_fund_days",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "guarantee_fund_percentage",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_instructivo_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_non_conforming_output_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "project_sub_contractor_tolerance_chart_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "abbreviation",
                table: "project");

            migrationBuilder.DropColumn(
                name: "partida_id",
                table: "phase_stage_sub_stage_sub_specialty");

            migrationBuilder.DropColumn(
                name: "sub_area_id",
                table: "lesson");

            migrationBuilder.DropColumn(
                name: "contractor_person_type_id",
                table: "contractor_email");

            migrationBuilder.DropColumn(
                name: "activation_token",
                table: "contractor");

            migrationBuilder.DropColumn(
                name: "activation_token_expiry",
                table: "contractor");

            migrationBuilder.DropColumn(
                name: "logo_file_url",
                table: "contractor");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "ac_actividades");

            migrationBuilder.DropColumn(
                name: "especialidad_id",
                table: "ac_actividades");


            migrationBuilder.AddColumn<string>(
                name: "apellido_nombre",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dni",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ruc",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "worker_id",
                table: "ss_alertas_emo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "emo_id",
                table: "ss_alertas_emo",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "contract_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_alertas_emo_worker_emos_emo_id",
                table: "ss_alertas_emo",
                column: "emo_id",
                principalTable: "worker_emos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_alertas_emo_workers_worker_id",
                table: "ss_alertas_emo",
                column: "worker_id",
                principalTable: "workers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
