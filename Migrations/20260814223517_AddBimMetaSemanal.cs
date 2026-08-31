using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBimMetaSemanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_gth_postulante_formulario_gth_puesto_convocatoria_gth_puest",
                table: "gth_postulante_formulario");

            migrationBuilder.DropForeignKey(
                name: "fk_gth_requerimiento_gth_puesto_gth_puesto_id",
                table: "gth_requerimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_motivo_motivo_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_tipo_tipo_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_cat_ocupacion_ocupacion_id",
                table: "workers");

            migrationBuilder.DropTable(
                name: "cat_jefatura");

            migrationBuilder.DropIndex(
                name: "ix_workers_ocupacion_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_ss_descanso_medico_motivo_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropIndex(
                name: "ix_gth_postulante_formulario_convocatoria_gth_puesto_id",
                table: "gth_postulante_formulario");

            migrationBuilder.DropIndex(
                name: "ix_gth_correo_destinatario_gth_correo_tipo_id_email",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "obra_oficina",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "establecimiento",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "medico_certifica",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "motivo",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "motivo_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "convocatoria_gth_puesto_id",
                table: "gth_postulante_formulario");

            migrationBuilder.RenameColumn(
                name: "gth_puesto_id",
                table: "gth_requerimiento",
                newName: "puesto_id");

            migrationBuilder.RenameIndex(
                name: "ix_gth_requerimiento_gth_puesto_id",
                table: "gth_requerimiento",
                newName: "ix_gth_requerimiento_puesto_id");

            migrationBuilder.AddColumn<int>(
                name: "categoria_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "obra_oficina_staff_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "puesto_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "obra_oficina_staff_id",
                table: "worker_vinculaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "cambio_riesgo",
                table: "worker_emo_convalidaciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "obra_oficina_staff_destino_id",
                table: "worker_emo_convalidaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "obra_oficina_staff_origen_id",
                table: "worker_emo_convalidaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puesto_destino",
                table: "worker_emo_convalidaciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puesto_origen",
                table: "worker_emo_convalidaciones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inspector_worker_id",
                table: "ssoma_inspeccion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "state",
                table: "ss_programacion_emos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "dni",
                table: "ss_medicos_ocupacionales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_autorizacion_firma",
                table: "ss_medicos_ocupacionales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firma_digital_url",
                table: "ss_medicos_ocupacionales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pin_firma_bloqueado_hasta",
                table: "ss_medicos_ocupacionales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pin_firma_hash",
                table: "ss_medicos_ocupacionales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pin_firma_intentos_fallidos",
                table: "ss_medicos_ocupacionales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "url_autorizacion_firmada",
                table: "ss_medicos_ocupacionales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "disponible_mi_salud",
                table: "ss_descanso_tipo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "nombre_corto",
                table: "ss_descanso_tipo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "orden",
                table: "ss_descanso_tipo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "caso_id",
                table: "ss_descanso_seguimiento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "confidencial",
                table: "ss_descanso_seguimiento",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "diagnostico_cie10_codigo",
                table: "ss_descanso_seguimiento",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puesto_trabajo_snapshot",
                table: "ss_descanso_seguimiento",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_id",
                table: "ss_descanso_seguimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "drive_id",
                table: "ss_descanso_medico_adjunto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_id",
                table: "ss_descanso_medico_adjunto",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "tipo_id",
                table: "ss_descanso_medico",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "caso_id",
                table: "ss_descanso_medico",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "diagnostico_cie10_codigo",
                table: "ss_descanso_medico",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "area_scope_id",
                table: "reunion_tema",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_id",
                table: "reunion_participante",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "reunion_participante_id",
                table: "reunion_acuerdo_responsable",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "estado_aceptacion",
                table: "reunion_acuerdo_responsable",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_respuesta",
                table: "reunion_acuerdo_responsable",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "reunion_acuerdo_responsable",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_id",
                table: "reunion_acuerdo_responsable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidencia_url",
                table: "reunion_acuerdo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requiere_aceptacion",
                table: "reunion_acuerdo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requiere_evidencia",
                table: "reunion_acuerdo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "reunion",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "area_scope_id",
                table: "reunion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lat",
                table: "project",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lng",
                table: "project",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "radio_geofence_metros",
                table: "project",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "residente_workers_id",
                table: "project",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "mostrar_en_boletin",
                table: "person",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "obra_oficina_staff_id",
                table: "lesson",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                table: "gth_tipo_requerimiento",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "categoria_id",
                table: "gth_requerimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reemplaza_worker_id",
                table: "gth_requerimiento",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "gth_correo_tipo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "principal_automatico",
                table: "gth_correo_tipo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "gth_correo_destinatario",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                table: "gth_correo_destinatario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "gth_correo_destinatario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre",
                table: "gth_correo_destinatario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "orden",
                table: "gth_correo_destinatario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "multitest_date_time",
                table: "gth_candidato",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "multitest_realizado",
                table: "gth_candidato",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "multitest_user_id",
                table: "gth_candidato",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ac_ranking_semanal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    semana = table.Column<DateOnly>(type: "date", nullable: false),
                    ies = table.Column<decimal>(type: "numeric", nullable: false),
                    comp_spi = table.Column<decimal>(type: "numeric", nullable: false),
                    comp_cierre = table.Column<decimal>(type: "numeric", nullable: false),
                    comp_inicio = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<int>(type: "integer", nullable: false),
                    completadas = table.Column<int>(type: "integer", nullable: false),
                    sin_compromisos = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ac_ranking_semanal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ac_tareo_enrolamiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    foto_url = table.Column<string>(type: "text", nullable: false),
                    consentimiento_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ac_tareo_enrolamiento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ac_tareo_registro",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_servidor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hora_dispositivo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    foto_url = table.Column<string>(type: "text", nullable: false),
                    foto_hash = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    lat = table.Column<decimal>(type: "numeric", nullable: true),
                    lng = table.Column<decimal>(type: "numeric", nullable: true),
                    precision_metros = table.Column<decimal>(type: "numeric", nullable: true),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    distancia_metros = table.Column<decimal>(type: "numeric", nullable: true),
                    face_match_score = table.Column<decimal>(type: "numeric", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    motivo_revision = table.Column<string>(type: "text", nullable: true),
                    revisado_por = table.Column<int>(type: "integer", nullable: true),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_origen = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ac_tareo_registro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bim_meta_semanal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    macro_actividad_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio_semana = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin_semana = table.Column<DateOnly>(type: "date", nullable: false),
                    meta_avance = table.Column<decimal>(type: "numeric", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_meta_semanal", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_meta_semanal_bim_macro_actividad_macro_actividad_id",
                        column: x => x.macro_actividad_id,
                        principalTable: "bim_macro_actividad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_meta_semanal_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    categoria_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    visible_solicitud_personal = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categoria", x => x.categoria_id);
                });

            migrationBuilder.CreateTable(
                name: "cie10_catalogo",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cie10_catalogo", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "gth_aprobacion_gg_estado",
                columns: table => new
                {
                    gth_aprobacion_gg_estado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_aprobacion_gg_estado", x => x.gth_aprobacion_gg_estado_id);
                });

            migrationBuilder.CreateTable(
                name: "gth_candidato_resultado",
                columns: table => new
                {
                    gth_candidato_resultado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_candidato_resultado", x => x.gth_candidato_resultado_id);
                });

            migrationBuilder.CreateTable(
                name: "gth_lugar_entrevista",
                columns: table => new
                {
                    gth_lugar_entrevista_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_lugar_entrevista", x => x.gth_lugar_entrevista_id);
                });

            migrationBuilder.CreateTable(
                name: "reunion_tema_puesto",
                columns: table => new
                {
                    reunion_tema_puesto_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reunion_tema_id = table.Column<int>(type: "integer", nullable: false),
                    puesto_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reunion_tema_puesto", x => x.reunion_tema_puesto_id);
                });

            migrationBuilder.CreateTable(
                name: "ss_convalidacion_firma_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    convalidacion_id = table.Column<int>(type: "integer", nullable: false),
                    medico_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    documento_hash = table.Column<string>(type: "text", nullable: false),
                    resultado = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_convalidacion_firma_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_descanso_carpeta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    link_url = table.Column<string>(type: "text", nullable: false),
                    folder_name = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_descanso_carpeta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_descanso_caso",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_apertura = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_cierre = table.Column<DateOnly>(type: "date", nullable: true),
                    alta_por_id = table.Column<int>(type: "integer", nullable: true),
                    alta_observaciones = table.Column<string>(type: "text", nullable: true),
                    fecha_reapertura = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_descanso_caso", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_descanso_caso_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_emo_correo_evento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_emo_correo_evento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_emo_correo_perfil",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_emo_correo_perfil", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_emo_correo_tipo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_emo_correo_tipo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_seguimiento_tipo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_seguimiento_tipo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workers_obra_oficina_staff",
                columns: table => new
                {
                    workers_obra_oficina_staff_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workers_obra_oficina_staff", x => x.workers_obra_oficina_staff_id);
                });

            migrationBuilder.CreateTable(
                name: "puesto",
                columns: table => new
                {
                    puesto_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puesto", x => x.puesto_id);
                    table.ForeignKey(
                        name: "fk_puesto_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categoria",
                        principalColumn: "categoria_id");
                });

            migrationBuilder.CreateTable(
                name: "gth_aprobacion_gg",
                columns: table => new
                {
                    gth_aprobacion_gg_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gth_solicitud_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    gth_aprobacion_gg_estado_id = table.Column<int>(type: "integer", nullable: false),
                    correo_envio = table.Column<string>(type: "text", nullable: true),
                    correo_copia = table.Column<string>(type: "text", nullable: true),
                    enviado_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reenviado_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decidido_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decidido_user_id = table.Column<int>(type: "integer", nullable: true),
                    comentario = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_aprobacion_gg", x => x.gth_aprobacion_gg_id);
                    table.ForeignKey(
                        name: "fk_gth_aprobacion_gg_gth_aprobacion_gg_estado_gth_aprobacion_g",
                        column: x => x.gth_aprobacion_gg_estado_id,
                        principalTable: "gth_aprobacion_gg_estado",
                        principalColumn: "gth_aprobacion_gg_estado_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gth_aprobacion_gg_gth_solicitud_gth_solicitud_id",
                        column: x => x.gth_solicitud_id,
                        principalTable: "gth_solicitud",
                        principalColumn: "gth_solicitud_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gth_candidato_evaluacion",
                columns: table => new
                {
                    gth_candidato_evaluacion_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gth_candidato_id = table.Column<int>(type: "integer", nullable: false),
                    gth_candidato_resultado_id = table.Column<int>(type: "integer", nullable: false),
                    comentario_entrevista = table.Column<string>(type: "text", nullable: true),
                    comentario_psicotecnico = table.Column<string>(type: "text", nullable: true),
                    comentario_recomendacion = table.Column<string>(type: "text", nullable: true),
                    agradecimiento_correo = table.Column<string>(type: "text", nullable: true),
                    agradecimiento_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    agradecimiento_user_id = table.Column<int>(type: "integer", nullable: true),
                    decision_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_user_id = table.Column<int>(type: "integer", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_candidato_evaluacion", x => x.gth_candidato_evaluacion_id);
                    table.ForeignKey(
                        name: "fk_gth_candidato_evaluacion_gth_candidato_gth_candidato_id",
                        column: x => x.gth_candidato_id,
                        principalTable: "gth_candidato",
                        principalColumn: "gth_candidato_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gth_candidato_evaluacion_gth_candidato_resultado_gth_candid",
                        column: x => x.gth_candidato_resultado_id,
                        principalTable: "gth_candidato_resultado",
                        principalColumn: "gth_candidato_resultado_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gth_entrevista",
                columns: table => new
                {
                    gth_entrevista_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gth_candidato_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    gth_lugar_entrevista_id = table.Column<int>(type: "integer", nullable: false),
                    correo_envio = table.Column<string>(type: "text", nullable: false),
                    enviado_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    enviado_user_id = table.Column<int>(type: "integer", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_entrevista", x => x.gth_entrevista_id);
                    table.ForeignKey(
                        name: "fk_gth_entrevista_gth_candidato_gth_candidato_id",
                        column: x => x.gth_candidato_id,
                        principalTable: "gth_candidato",
                        principalColumn: "gth_candidato_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gth_entrevista_gth_lugar_entrevista_gth_lugar_entrevista_id",
                        column: x => x.gth_lugar_entrevista_id,
                        principalTable: "gth_lugar_entrevista",
                        principalColumn: "gth_lugar_entrevista_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ss_emo_correo_destinatario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    editable = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_emo_correo_destinatario", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_emo_correo_destinatario_ss_emo_correo_tipo_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "ss_emo_correo_tipo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gth_aprobacion_gg_detalle",
                columns: table => new
                {
                    gth_aprobacion_gg_detalle_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gth_aprobacion_gg_id = table.Column<int>(type: "integer", nullable: false),
                    gth_requerimiento_id = table.Column<int>(type: "integer", nullable: false),
                    aprobado = table.Column<bool>(type: "boolean", nullable: true),
                    decidido_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gth_aprobacion_gg_detalle", x => x.gth_aprobacion_gg_detalle_id);
                    table.ForeignKey(
                        name: "fk_gth_aprobacion_gg_detalle_gth_aprobacion_gg_gth_aprobacion_",
                        column: x => x.gth_aprobacion_gg_id,
                        principalTable: "gth_aprobacion_gg",
                        principalColumn: "gth_aprobacion_gg_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gth_aprobacion_gg_detalle_gth_requerimiento_gth_requerimien",
                        column: x => x.gth_requerimiento_id,
                        principalTable: "gth_requerimiento",
                        principalColumn: "gth_requerimiento_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ss_emo_correo_regla",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    evento_id = table.Column<int>(type: "integer", nullable: false),
                    perfil_id = table.Column<int>(type: "integer", nullable: false),
                    destinatario_id = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_emo_correo_regla", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_emo_correo_regla_ss_emo_correo_destinatario_destinatario",
                        column: x => x.destinatario_id,
                        principalTable: "ss_emo_correo_destinatario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_emo_correo_regla_ss_emo_correo_evento_evento_id",
                        column: x => x.evento_id,
                        principalTable: "ss_emo_correo_evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_emo_correo_regla_ss_emo_correo_perfil_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "ss_emo_correo_perfil",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workers_categoria_id",
                table: "workers",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_workers_obra_oficina_staff_id",
                table: "workers",
                column: "obra_oficina_staff_id");

            migrationBuilder.CreateIndex(
                name: "ix_workers_puesto_id",
                table: "workers",
                column: "puesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_inspector_worker_id",
                table: "ssoma_inspeccion",
                column: "inspector_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_seguimiento_diagnostico_cie10_codigo",
                table: "ss_descanso_seguimiento",
                column: "diagnostico_cie10_codigo");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_seguimiento_tipo_id",
                table: "ss_descanso_seguimiento",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_medico_caso_id",
                table: "ss_descanso_medico",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_medico_diagnostico_cie10_codigo",
                table: "ss_descanso_medico",
                column: "diagnostico_cie10_codigo");

            migrationBuilder.CreateIndex(
                name: "ix_gth_tipo_requerimiento_codigo",
                table: "gth_tipo_requerimiento",
                column: "codigo",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_requerimiento_categoria_id",
                table: "gth_requerimiento",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_requerimiento_reemplaza_worker_id",
                table: "gth_requerimiento",
                column: "reemplaza_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_correo_destinatario_gth_correo_tipo_id_email",
                table: "gth_correo_destinatario",
                columns: new[] { "gth_correo_tipo_id", "email" },
                unique: true,
                filter: "state = true AND codigo IS NULL AND email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bim_meta_semanal_macro_actividad_id",
                table: "bim_meta_semanal",
                column: "macro_actividad_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_meta_semanal_unico",
                table: "bim_meta_semanal",
                columns: new[] { "project_id", "macro_actividad_id", "fecha_inicio_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_gth_aprobacion_gg_estado_id",
                table: "gth_aprobacion_gg",
                column: "gth_aprobacion_gg_estado_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_gth_solicitud_id",
                table: "gth_aprobacion_gg",
                column: "gth_solicitud_id",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_token",
                table: "gth_aprobacion_gg",
                column: "token",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_detalle_gth_aprobacion_gg_id_gth_requerim",
                table: "gth_aprobacion_gg_detalle",
                columns: new[] { "gth_aprobacion_gg_id", "gth_requerimiento_id" },
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_detalle_gth_requerimiento_id",
                table: "gth_aprobacion_gg_detalle",
                column: "gth_requerimiento_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_aprobacion_gg_estado_codigo",
                table: "gth_aprobacion_gg_estado",
                column: "codigo",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_candidato_evaluacion_gth_candidato_id",
                table: "gth_candidato_evaluacion",
                column: "gth_candidato_id",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_candidato_evaluacion_gth_candidato_resultado_id",
                table: "gth_candidato_evaluacion",
                column: "gth_candidato_resultado_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_candidato_resultado_codigo",
                table: "gth_candidato_resultado",
                column: "codigo",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_entrevista_gth_candidato_id",
                table: "gth_entrevista",
                column: "gth_candidato_id",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_gth_entrevista_gth_lugar_entrevista_id",
                table: "gth_entrevista",
                column: "gth_lugar_entrevista_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_lugar_entrevista_nombre",
                table: "gth_lugar_entrevista",
                column: "nombre",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_puesto_categoria_id",
                table: "puesto",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_caso_worker_id",
                table: "ss_descanso_caso",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_emo_correo_destinatario_tipo_id",
                table: "ss_emo_correo_destinatario",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_emo_correo_regla_destinatario_id",
                table: "ss_emo_correo_regla",
                column: "destinatario_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_emo_correo_regla_evento_id",
                table: "ss_emo_correo_regla",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_emo_correo_regla_perfil_id",
                table: "ss_emo_correo_regla",
                column: "perfil_id");

            migrationBuilder.AddForeignKey(
                name: "fk_gth_requerimiento_categoria_categoria_id",
                table: "gth_requerimiento",
                column: "categoria_id",
                principalTable: "categoria",
                principalColumn: "categoria_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_gth_requerimiento_puesto_puesto_id",
                table: "gth_requerimiento",
                column: "puesto_id",
                principalTable: "puesto",
                principalColumn: "puesto_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_gth_requerimiento_workers_reemplaza_worker_id",
                table: "gth_requerimiento",
                column: "reemplaza_worker_id",
                principalTable: "workers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_medico_cie10_catalogo_diagnostico_cie10_codigo",
                table: "ss_descanso_medico",
                column: "diagnostico_cie10_codigo",
                principalTable: "cie10_catalogo",
                principalColumn: "codigo");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_caso_caso_id",
                table: "ss_descanso_medico",
                column: "caso_id",
                principalTable: "ss_descanso_caso",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_tipo_tipo_id",
                table: "ss_descanso_medico",
                column: "tipo_id",
                principalTable: "ss_descanso_tipo",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_seguimiento_cie10_catalogo_diagnostico_cie10_co",
                table: "ss_descanso_seguimiento",
                column: "diagnostico_cie10_codigo",
                principalTable: "cie10_catalogo",
                principalColumn: "codigo");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_seguimiento_ss_seguimiento_tipo_tipo_id",
                table: "ss_descanso_seguimiento",
                column: "tipo_id",
                principalTable: "ss_seguimiento_tipo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ssoma_inspeccion_workers_inspector_worker_id",
                table: "ssoma_inspeccion",
                column: "inspector_worker_id",
                principalTable: "workers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_categoria_categoria_id",
                table: "workers",
                column: "categoria_id",
                principalTable: "categoria",
                principalColumn: "categoria_id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_puesto_puesto_id",
                table: "workers",
                column: "puesto_id",
                principalTable: "puesto",
                principalColumn: "puesto_id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_workers_obra_oficina_staff_obra_oficina_staff_id",
                table: "workers",
                column: "obra_oficina_staff_id",
                principalTable: "workers_obra_oficina_staff",
                principalColumn: "workers_obra_oficina_staff_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_gth_requerimiento_categoria_categoria_id",
                table: "gth_requerimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_gth_requerimiento_puesto_puesto_id",
                table: "gth_requerimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_gth_requerimiento_workers_reemplaza_worker_id",
                table: "gth_requerimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_medico_cie10_catalogo_diagnostico_cie10_codigo",
                table: "ss_descanso_medico");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_caso_caso_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_tipo_tipo_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_seguimiento_cie10_catalogo_diagnostico_cie10_co",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_descanso_seguimiento_ss_seguimiento_tipo_tipo_id",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropForeignKey(
                name: "fk_ssoma_inspeccion_workers_inspector_worker_id",
                table: "ssoma_inspeccion");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_categoria_categoria_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_puesto_puesto_id",
                table: "workers");

            migrationBuilder.DropForeignKey(
                name: "fk_workers_workers_obra_oficina_staff_obra_oficina_staff_id",
                table: "workers");

            migrationBuilder.DropTable(
                name: "ac_ranking_semanal");

            migrationBuilder.DropTable(
                name: "ac_tareo_enrolamiento");

            migrationBuilder.DropTable(
                name: "ac_tareo_registro");

            migrationBuilder.DropTable(
                name: "bim_meta_semanal");

            migrationBuilder.DropTable(
                name: "cie10_catalogo");

            migrationBuilder.DropTable(
                name: "gth_aprobacion_gg_detalle");

            migrationBuilder.DropTable(
                name: "gth_candidato_evaluacion");

            migrationBuilder.DropTable(
                name: "gth_entrevista");

            migrationBuilder.DropTable(
                name: "puesto");

            migrationBuilder.DropTable(
                name: "reunion_tema_puesto");

            migrationBuilder.DropTable(
                name: "ss_convalidacion_firma_log");

            migrationBuilder.DropTable(
                name: "ss_descanso_carpeta");

            migrationBuilder.DropTable(
                name: "ss_descanso_caso");

            migrationBuilder.DropTable(
                name: "ss_emo_correo_regla");

            migrationBuilder.DropTable(
                name: "ss_seguimiento_tipo");

            migrationBuilder.DropTable(
                name: "workers_obra_oficina_staff");

            migrationBuilder.DropTable(
                name: "gth_aprobacion_gg");

            migrationBuilder.DropTable(
                name: "gth_candidato_resultado");

            migrationBuilder.DropTable(
                name: "gth_lugar_entrevista");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "ss_emo_correo_destinatario");

            migrationBuilder.DropTable(
                name: "ss_emo_correo_evento");

            migrationBuilder.DropTable(
                name: "ss_emo_correo_perfil");

            migrationBuilder.DropTable(
                name: "gth_aprobacion_gg_estado");

            migrationBuilder.DropTable(
                name: "ss_emo_correo_tipo");

            migrationBuilder.DropIndex(
                name: "ix_workers_categoria_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_workers_obra_oficina_staff_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_workers_puesto_id",
                table: "workers");

            migrationBuilder.DropIndex(
                name: "ix_ssoma_inspeccion_inspector_worker_id",
                table: "ssoma_inspeccion");

            migrationBuilder.DropIndex(
                name: "ix_ss_descanso_seguimiento_diagnostico_cie10_codigo",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropIndex(
                name: "ix_ss_descanso_seguimiento_tipo_id",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropIndex(
                name: "ix_ss_descanso_medico_caso_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropIndex(
                name: "ix_ss_descanso_medico_diagnostico_cie10_codigo",
                table: "ss_descanso_medico");

            migrationBuilder.DropIndex(
                name: "ix_gth_tipo_requerimiento_codigo",
                table: "gth_tipo_requerimiento");

            migrationBuilder.DropIndex(
                name: "ix_gth_requerimiento_categoria_id",
                table: "gth_requerimiento");

            migrationBuilder.DropIndex(
                name: "ix_gth_requerimiento_reemplaza_worker_id",
                table: "gth_requerimiento");

            migrationBuilder.DropIndex(
                name: "ix_gth_correo_destinatario_gth_correo_tipo_id_email",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "obra_oficina_staff_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "puesto_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "obra_oficina_staff_id",
                table: "worker_vinculaciones");

            migrationBuilder.DropColumn(
                name: "cambio_riesgo",
                table: "worker_emo_convalidaciones");

            migrationBuilder.DropColumn(
                name: "obra_oficina_staff_destino_id",
                table: "worker_emo_convalidaciones");

            migrationBuilder.DropColumn(
                name: "obra_oficina_staff_origen_id",
                table: "worker_emo_convalidaciones");

            migrationBuilder.DropColumn(
                name: "puesto_destino",
                table: "worker_emo_convalidaciones");

            migrationBuilder.DropColumn(
                name: "puesto_origen",
                table: "worker_emo_convalidaciones");

            migrationBuilder.DropColumn(
                name: "inspector_worker_id",
                table: "ssoma_inspeccion");

            migrationBuilder.DropColumn(
                name: "state",
                table: "ss_programacion_emos");

            migrationBuilder.DropColumn(
                name: "dni",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "fecha_autorizacion_firma",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "firma_digital_url",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "pin_firma_bloqueado_hasta",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "pin_firma_hash",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "pin_firma_intentos_fallidos",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "url_autorizacion_firmada",
                table: "ss_medicos_ocupacionales");

            migrationBuilder.DropColumn(
                name: "disponible_mi_salud",
                table: "ss_descanso_tipo");

            migrationBuilder.DropColumn(
                name: "nombre_corto",
                table: "ss_descanso_tipo");

            migrationBuilder.DropColumn(
                name: "orden",
                table: "ss_descanso_tipo");

            migrationBuilder.DropColumn(
                name: "caso_id",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropColumn(
                name: "confidencial",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropColumn(
                name: "diagnostico_cie10_codigo",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropColumn(
                name: "puesto_trabajo_snapshot",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropColumn(
                name: "tipo_id",
                table: "ss_descanso_seguimiento");

            migrationBuilder.DropColumn(
                name: "drive_id",
                table: "ss_descanso_medico_adjunto");

            migrationBuilder.DropColumn(
                name: "item_id",
                table: "ss_descanso_medico_adjunto");

            migrationBuilder.DropColumn(
                name: "caso_id",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "diagnostico_cie10_codigo",
                table: "ss_descanso_medico");

            migrationBuilder.DropColumn(
                name: "area_scope_id",
                table: "reunion_tema");

            migrationBuilder.DropColumn(
                name: "worker_id",
                table: "reunion_participante");

            migrationBuilder.DropColumn(
                name: "estado_aceptacion",
                table: "reunion_acuerdo_responsable");

            migrationBuilder.DropColumn(
                name: "fecha_respuesta",
                table: "reunion_acuerdo_responsable");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "reunion_acuerdo_responsable");

            migrationBuilder.DropColumn(
                name: "worker_id",
                table: "reunion_acuerdo_responsable");

            migrationBuilder.DropColumn(
                name: "evidencia_url",
                table: "reunion_acuerdo");

            migrationBuilder.DropColumn(
                name: "requiere_aceptacion",
                table: "reunion_acuerdo");

            migrationBuilder.DropColumn(
                name: "requiere_evidencia",
                table: "reunion_acuerdo");

            migrationBuilder.DropColumn(
                name: "area_scope_id",
                table: "reunion");

            migrationBuilder.DropColumn(
                name: "lat",
                table: "project");

            migrationBuilder.DropColumn(
                name: "lng",
                table: "project");

            migrationBuilder.DropColumn(
                name: "radio_geofence_metros",
                table: "project");

            migrationBuilder.DropColumn(
                name: "residente_workers_id",
                table: "project");

            migrationBuilder.DropColumn(
                name: "mostrar_en_boletin",
                table: "person");

            migrationBuilder.DropColumn(
                name: "obra_oficina_staff_id",
                table: "lesson");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "gth_tipo_requerimiento");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "gth_requerimiento");

            migrationBuilder.DropColumn(
                name: "reemplaza_worker_id",
                table: "gth_requerimiento");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "gth_correo_tipo");

            migrationBuilder.DropColumn(
                name: "principal_automatico",
                table: "gth_correo_tipo");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "nombre",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "orden",
                table: "gth_correo_destinatario");

            migrationBuilder.DropColumn(
                name: "multitest_date_time",
                table: "gth_candidato");

            migrationBuilder.DropColumn(
                name: "multitest_realizado",
                table: "gth_candidato");

            migrationBuilder.DropColumn(
                name: "multitest_user_id",
                table: "gth_candidato");

            migrationBuilder.RenameColumn(
                name: "puesto_id",
                table: "gth_requerimiento",
                newName: "gth_puesto_id");

            migrationBuilder.RenameIndex(
                name: "ix_gth_requerimiento_puesto_id",
                table: "gth_requerimiento",
                newName: "ix_gth_requerimiento_gth_puesto_id");

            migrationBuilder.AddColumn<string>(
                name: "obra_oficina",
                table: "workers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "tipo_id",
                table: "ss_descanso_medico",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "establecimiento",
                table: "ss_descanso_medico",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medico_certifica",
                table: "ss_descanso_medico",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo",
                table: "ss_descanso_medico",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "motivo_id",
                table: "ss_descanso_medico",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "ss_descanso_medico",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "reunion_participante_id",
                table: "reunion_acuerdo_responsable",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "reunion",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "convocatoria_gth_puesto_id",
                table: "gth_postulante_formulario",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "gth_correo_destinatario",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "cat_jefatura",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cat_jefatura", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workers_ocupacion_id",
                table: "workers",
                column: "ocupacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_medico_motivo_id",
                table: "ss_descanso_medico",
                column: "motivo_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_postulante_formulario_convocatoria_gth_puesto_id",
                table: "gth_postulante_formulario",
                column: "convocatoria_gth_puesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_gth_correo_destinatario_gth_correo_tipo_id_email",
                table: "gth_correo_destinatario",
                columns: new[] { "gth_correo_tipo_id", "email" },
                unique: true,
                filter: "state = true");

            migrationBuilder.AddForeignKey(
                name: "fk_gth_postulante_formulario_gth_puesto_convocatoria_gth_puest",
                table: "gth_postulante_formulario",
                column: "convocatoria_gth_puesto_id",
                principalTable: "gth_puesto",
                principalColumn: "gth_puesto_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_gth_requerimiento_gth_puesto_gth_puesto_id",
                table: "gth_requerimiento",
                column: "gth_puesto_id",
                principalTable: "gth_puesto",
                principalColumn: "gth_puesto_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_motivo_motivo_id",
                table: "ss_descanso_medico",
                column: "motivo_id",
                principalTable: "ss_descanso_motivo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_descanso_medico_ss_descanso_tipo_tipo_id",
                table: "ss_descanso_medico",
                column: "tipo_id",
                principalTable: "ss_descanso_tipo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_workers_cat_ocupacion_ocupacion_id",
                table: "workers",
                column: "ocupacion_id",
                principalTable: "cat_ocupacion",
                principalColumn: "id");
        }
    }
}
