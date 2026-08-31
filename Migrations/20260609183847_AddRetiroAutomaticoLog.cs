using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRetiroAutomaticoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS layer CASCADE;");
            // partida kept: used by AccidentesIncidentes flash report queries via raw SQL
            migrationBuilder.Sql("DROP TABLE IF EXISTS phase CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS phase_stage_sub_stage_sub_specialty CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS psss_scope CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS psss_template CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS psss_template_detail CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stage CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS sub_specialty CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS sub_stage CASCADE;");

            migrationBuilder.Sql("ALTER TABLE ga_solicitud_salida DROP COLUMN IF EXISTS estado_aprobacion;");
            migrationBuilder.Sql("ALTER TABLE ga_solicitud_salida DROP COLUMN IF EXISTS estado_rendicion;");
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson' AND column_name='phase_stage_sub_stage_sub_specialty_id')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson' AND column_name='reviewed_by_user_id') THEN
                        ALTER TABLE lesson RENAME COLUMN phase_stage_sub_stage_sub_specialty_id TO reviewed_by_user_id;
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='workers' AND column_name='area_scope_id') THEN
                        ALTER TABLE workers ADD COLUMN area_scope_id integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_item_empresa' AND column_name='es_mensual') THEN
                        ALTER TABLE ss_item_empresa ADD COLUMN es_mensual boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_hab_empresa' AND column_name='motivo_rechazo') THEN
                        ALTER TABLE ss_hab_empresa ADD COLUMN motivo_rechazo text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_hab_documento_version' AND column_name='enviado') THEN
                        ALTER TABLE ss_hab_documento_version ADD COLUMN enviado boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_hab_documento_version' AND column_name='fecha_envio') THEN
                        ALTER TABLE ss_hab_documento_version ADD COLUMN fecha_envio timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='project_sub_contractor' AND column_name='finish_protection_status_id') THEN
                        ALTER TABLE project_sub_contractor ADD COLUMN finish_protection_status_id integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='project_sub_contractor' AND column_name='non_conforming_output_status_id') THEN
                        ALTER TABLE project_sub_contractor ADD COLUMN non_conforming_output_status_id integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='project_sub_contractor' AND column_name='tolerance_chart_status_id') THEN
                        ALTER TABLE project_sub_contractor ADD COLUMN tolerance_chart_status_id integer;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='project' AND column_name='operativo') THEN
                        ALTER TABLE project ADD COLUMN operativo boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson_area' AND column_name='include_as_independent') THEN
                        ALTER TABLE lesson_area ADD COLUMN include_as_independent boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson_area' AND column_name='include_descendants') THEN
                        ALTER TABLE lesson_area ADD COLUMN include_descendants boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson_area' AND column_name='include_in_form') THEN
                        ALTER TABLE lesson_area ADD COLUMN include_in_form boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson' AND column_name='approval_status') THEN
                        ALTER TABLE lesson ADD COLUMN approval_status text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson' AND column_name='rejection_comment') THEN
                        ALTER TABLE lesson ADD COLUMN rejection_comment text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='lesson' AND column_name='reviewed_at') THEN
                        ALTER TABLE lesson ADD COLUMN reviewed_at timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ga_solicitud_salida' AND column_name='estado_aprobacion_id') THEN
                        ALTER TABLE ga_solicitud_salida ADD COLUMN estado_aprobacion_id integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ga_solicitud_salida' AND column_name='estado_rendicion_id') THEN
                        ALTER TABLE ga_solicitud_salida ADD COLUMN estado_rendicion_id integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ga_rendicion' AND column_name='numero_planilla') THEN
                        ALTER TABLE ga_rendicion ADD COLUMN numero_planilla integer;
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS lesson_jefe_reminder (
                    lesson_jefe_reminder_id serial PRIMARY KEY,
                    worker_id integer NOT NULL,
                    active boolean NOT NULL,
                    state boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone
                );
                CREATE TABLE IF NOT EXISTS ss_hab_documento_archivo (
                    id serial PRIMARY KEY,
                    version_id integer NOT NULL REFERENCES ss_hab_documento_version(id) ON DELETE CASCADE,
                    archivo_url text NOT NULL,
                    nombre_archivo text,
                    es_zip boolean NOT NULL,
                    zip_contenido text,
                    orden integer NOT NULL,
                    created_at timestamp with time zone
                );
                CREATE TABLE IF NOT EXISTS ss_retiro_automatico_log (
                    id serial PRIMARY KEY,
                    worker_id integer NOT NULL,
                    empresa_id integer,
                    motivo text,
                    ejecutado_en timestamp with time zone NOT NULL,
                    tipo_retiro varchar(50) NOT NULL,
                    dias_gracia integer,
                    entregables_vencidos text
                );
                CREATE TABLE IF NOT EXISTS ssoma_paso (
                    id serial PRIMARY KEY,
                    proyecto_id integer,
                    plantilla_id integer,
                    nombre text NOT NULL,
                    anio integer NOT NULL,
                    mes_inicio integer NOT NULL,
                    es_plantilla boolean NOT NULL,
                    estado text NOT NULL,
                    aprobado_por integer,
                    aprobado_en timestamp with time zone,
                    created_by integer,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone
                );
                CREATE TABLE IF NOT EXISTS ssoma_paso_auditoria (
                    id serial PRIMARY KEY,
                    tipo text NOT NULL,
                    entidad text NOT NULL,
                    entidad_id integer NOT NULL,
                    paso_id integer NOT NULL,
                    descripcion text,
                    motivo text,
                    valor_anterior jsonb,
                    valor_nuevo jsonb,
                    usuario_id integer,
                    created_at timestamp with time zone NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_paso_categoria (
                    id serial PRIMARY KEY,
                    nombre text NOT NULL,
                    ambito text NOT NULL,
                    icono text,
                    activo boolean NOT NULL
                );
                CREATE TABLE IF NOT EXISTS work_item_category_anexo3_clause (
                    work_item_category_anexo3_clause_id serial PRIMARY KEY,
                    work_item_category_id integer NOT NULL REFERENCES work_item_category(work_item_category_id) ON DELETE CASCADE,
                    clause_text text NOT NULL,
                    sort_order integer NOT NULL,
                    state boolean NOT NULL,
                    created_datetime timestamp with time zone NOT NULL,
                    created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone,
                    updated_user_id integer
                );
                CREATE TABLE IF NOT EXISTS work_item_category_anexo4_clause (
                    work_item_category_anexo4_clause_id serial PRIMARY KEY,
                    work_item_category_id integer NOT NULL REFERENCES work_item_category(work_item_category_id) ON DELETE CASCADE,
                    clause_text text NOT NULL,
                    sort_order integer NOT NULL,
                    state boolean NOT NULL,
                    created_datetime timestamp with time zone NOT NULL,
                    created_user_id integer NOT NULL,
                    updated_datetime timestamp with time zone,
                    updated_user_id integer
                );
                CREATE TABLE IF NOT EXISTS ssoma_paso_actividad (
                    id serial PRIMARY KEY,
                    paso_id integer NOT NULL REFERENCES ssoma_paso(id) ON DELETE CASCADE,
                    categoria_id integer NOT NULL REFERENCES ssoma_paso_categoria(id) ON DELETE CASCADE,
                    nombre text NOT NULL,
                    descripcion text,
                    alcance text,
                    frecuencia text NOT NULL,
                    responsable_id integer,
                    responsable_texto text,
                    mes_inicio integer NOT NULL,
                    mes_fin integer NOT NULL,
                    cantidad_planificada integer NOT NULL,
                    horas numeric,
                    recursos text,
                    indicador text NOT NULL,
                    meta text NOT NULL,
                    orden integer,
                    activo boolean NOT NULL,
                    deleted_at timestamp with time zone,
                    deleted_by integer,
                    motivo_eliminacion text
                );
                CREATE TABLE IF NOT EXISTS ssoma_paso_ejecucion (
                    id serial PRIMARY KEY,
                    actividad_id integer NOT NULL REFERENCES ssoma_paso_actividad(id) ON DELETE CASCADE,
                    fecha_programada date NOT NULL,
                    fecha_verificacion date,
                    fecha_ejecutada date,
                    fecha_reprogramada date,
                    motivo_reprogramacion text,
                    estado text NOT NULL,
                    observaciones text,
                    participantes_count integer,
                    evidencia_nombre text,
                    evidencia_url text,
                    evidencia_sp_id text,
                    registrado_por integer,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone
                );
                CREATE INDEX IF NOT EXISTS ix_ss_hab_documento_archivo_version_id ON ss_hab_documento_archivo(version_id);
                CREATE INDEX IF NOT EXISTS ix_ssoma_paso_actividad_categoria_id ON ssoma_paso_actividad(categoria_id);
                CREATE INDEX IF NOT EXISTS ix_ssoma_paso_actividad_paso_id ON ssoma_paso_actividad(paso_id);
                CREATE INDEX IF NOT EXISTS ix_ssoma_paso_ejecucion_actividad_id ON ssoma_paso_ejecucion(actividad_id);
                CREATE INDEX IF NOT EXISTS ix_work_item_category_anexo3_clause_work_item_category_id ON work_item_category_anexo3_clause(work_item_category_id);
                CREATE INDEX IF NOT EXISTS ix_work_item_category_anexo4_clause_work_item_category_id ON work_item_category_anexo4_clause(work_item_category_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_jefe_reminder");

            migrationBuilder.DropTable(
                name: "ss_hab_documento_archivo");

            migrationBuilder.DropTable(
                name: "ss_retiro_automatico_log");

            migrationBuilder.DropTable(
                name: "ssoma_paso_auditoria");

            migrationBuilder.DropTable(
                name: "ssoma_paso_ejecucion");

            migrationBuilder.DropTable(
                name: "work_item_category_anexo3_clause");

            migrationBuilder.DropTable(
                name: "work_item_category_anexo4_clause");

            migrationBuilder.DropTable(
                name: "ssoma_paso_actividad");

            migrationBuilder.DropTable(
                name: "ssoma_paso_categoria");

            migrationBuilder.DropTable(
                name: "ssoma_paso");

            migrationBuilder.DropColumn(
                name: "area_scope_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "es_mensual",
                table: "ss_item_empresa");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "ss_hab_empresa");

            migrationBuilder.DropColumn(
                name: "enviado",
                table: "ss_hab_documento_version");

            migrationBuilder.DropColumn(
                name: "fecha_envio",
                table: "ss_hab_documento_version");

            migrationBuilder.DropColumn(
                name: "finish_protection_status_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "non_conforming_output_status_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "tolerance_chart_status_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "operativo",
                table: "project");

            migrationBuilder.DropColumn(
                name: "include_as_independent",
                table: "lesson_area");

            migrationBuilder.DropColumn(
                name: "include_descendants",
                table: "lesson_area");

            migrationBuilder.DropColumn(
                name: "include_in_form",
                table: "lesson_area");

            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "lesson");

            migrationBuilder.DropColumn(
                name: "rejection_comment",
                table: "lesson");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "lesson");

            migrationBuilder.DropColumn(
                name: "estado_aprobacion_id",
                table: "ga_solicitud_salida");

            migrationBuilder.DropColumn(
                name: "estado_rendicion_id",
                table: "ga_solicitud_salida");

            migrationBuilder.DropColumn(
                name: "numero_planilla",
                table: "ga_rendicion");

            migrationBuilder.RenameColumn(
                name: "reviewed_by_user_id",
                table: "lesson",
                newName: "phase_stage_sub_stage_sub_specialty_id");

            migrationBuilder.AddColumn<string>(
                name: "estado_aprobacion",
                table: "ga_solicitud_salida",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "estado_rendicion",
                table: "ga_solicitud_salida",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "layer",
                columns: table => new
                {
                    layer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    layer_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_layer", x => x.layer_id);
                });

            migrationBuilder.CreateTable(
                name: "partida",
                columns: table => new
                {
                    partida_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    partida_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partida", x => x.partida_id);
                });

            migrationBuilder.CreateTable(
                name: "phase",
                columns: table => new
                {
                    phase_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    phase_order = table.Column<int>(type: "integer", nullable: true),
                    phase_description = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phase", x => x.phase_id);
                });

            migrationBuilder.CreateTable(
                name: "phase_stage_sub_stage_sub_specialty",
                columns: table => new
                {
                    phase_stage_sub_stage_sub_specialty_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    layer_id = table.Column<int>(type: "integer", nullable: true),
                    partida_id = table.Column<int>(type: "integer", nullable: true),
                    phase_id = table.Column<int>(type: "integer", nullable: false),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    sub_specialty_id = table.Column<int>(type: "integer", nullable: true),
                    sub_stage_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phase_stage_sub_stage_sub_specialty", x => x.phase_stage_sub_stage_sub_specialty_id);
                });

            migrationBuilder.CreateTable(
                name: "psss_scope",
                columns: table => new
                {
                    psss_scope_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    area_id = table.Column<int>(type: "integer", nullable: true),
                    phase_stage_sub_stage_sub_specialty_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    sub_area_id = table.Column<int>(type: "integer", nullable: true)
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
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    template_name = table.Column<string>(type: "text", nullable: false),
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
                    phase_stage_sub_stage_sub_specialty_id = table.Column<int>(type: "integer", nullable: false),
                    psss_template_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_psss_template_detail", x => x.psss_template_detail_id);
                });

            migrationBuilder.CreateTable(
                name: "stage",
                columns: table => new
                {
                    stage_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    stage_description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stage", x => x.stage_id);
                });

            migrationBuilder.CreateTable(
                name: "sub_specialty",
                columns: table => new
                {
                    sub_specialty_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    sub_specialty_description = table.Column<string>(type: "text", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_specialty", x => x.sub_specialty_id);
                });

            migrationBuilder.CreateTable(
                name: "sub_stage",
                columns: table => new
                {
                    sub_stage_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    sub_stage_description = table.Column<string>(type: "text", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_stage", x => x.sub_stage_id);
                });
        }
    }
}
