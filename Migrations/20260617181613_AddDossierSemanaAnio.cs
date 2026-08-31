using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDossierSemanaAnio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "anios_experiencia",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_category_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_lesson_jefe_id",
                table: "workers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "contract_modality_id",
                table: "work_item_category_clause",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "work_specialty_id",
                table: "work_item",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "user_project",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "worker_id",
                table: "user_project",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "anio",
                table: "ssoma_paso",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "adjudicacion_folder_name",
                table: "project_sub_contractor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "arrival_observation",
                table: "project_sub_contractor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "includes_carta_fianza",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "payment_days",
                table: "project_sub_contractor",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "step6_signed_costos",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "step6_signed_gerente_general",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "step6_signed_gerente_inmobiliario",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "work_specialty_id",
                table: "project_sub_contractor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "contador_penalidad",
                table: "project",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "costos_cronograma",
                columns: table => new
                {
                    costos_cronograma_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_sub_contractor_id = table.Column<int>(type: "integer", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_costos_cronograma", x => x.costos_cronograma_id);
                });

            migrationBuilder.CreateTable(
                name: "costos_cronograma_actividad",
                columns: table => new
                {
                    costos_cronograma_actividad_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_costos_cronograma_actividad", x => x.costos_cronograma_actividad_id);
                });

            migrationBuilder.CreateTable(
                name: "costos_cronograma_actividad_nodo",
                columns: table => new
                {
                    costos_cronograma_actividad_nodo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    costos_cronograma_id = table.Column<int>(type: "integer", nullable: false),
                    costos_cronograma_actividad_id = table.Column<int>(type: "integer", nullable: false),
                    costos_cronograma_actividad_nodo_padre_id = table.Column<int>(type: "integer", nullable: true),
                    costos_cronograma_nodo_orden = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_costos_cronograma_actividad_nodo", x => x.costos_cronograma_actividad_nodo_id);
                });

            migrationBuilder.CreateTable(
                name: "project_adjudicacion_folder",
                columns: table => new
                {
                    project_adjudicacion_folder_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    link_url = table.Column<string>(type: "text", nullable: false),
                    drive_id = table.Column<string>(type: "text", nullable: false),
                    folder_id = table.Column<string>(type: "text", nullable: false),
                    folder_name = table.Column<string>(type: "text", nullable: true),
                    web_url = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_adjudicacion_folder", x => x.project_adjudicacion_folder_id);
                });

            migrationBuilder.CreateTable(
                name: "ss_dossier_semana",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contributor_id = table.Column<int>(type: "integer", nullable: false),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    numero_semana = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    obs_revisor = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_dossier_semana", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_tipo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    ambito = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_tipo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt_criterio_verificacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pregunta = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt_criterio_verificacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_paso_ejecucion_archivo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ejecucion_id = table.Column<int>(type: "integer", nullable: false),
                    archivo_url = table.Column<string>(type: "text", nullable: false),
                    archivo_nombre = table.Column<string>(type: "text", nullable: false),
                    archivo_sp_id = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_paso_ejecucion_archivo", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_paso_ejecucion_archivo_ssoma_paso_ejecucion_ejecucion",
                        column: x => x.ejecucion_id,
                        principalTable: "ssoma_paso_ejecucion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_pet",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    sharepoint_url = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_pet", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_rac_categoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    ambito = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_rac_categoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_rac_infraccion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    factor_uit = table.Column<decimal>(type: "numeric", nullable: true),
                    monto_fijo = table.Column<decimal>(type: "numeric", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_rac_infraccion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_uit_anio",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_uit_anio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_colindancia",
                columns: table => new
                {
                    vecino_colindancia_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_colindancia", x => x.vecino_colindancia_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_compromiso_estado",
                columns: table => new
                {
                    vecino_compromiso_estado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_compromiso_estado", x => x.vecino_compromiso_estado_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_entregable_estado",
                columns: table => new
                {
                    vecino_entregable_estado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_entregable_estado", x => x.vecino_entregable_estado_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_entregable_tipo",
                columns: table => new
                {
                    vecino_entregable_tipo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_entregable_tipo", x => x.vecino_entregable_tipo_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_solicitud_estado",
                columns: table => new
                {
                    vecino_solicitud_estado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_solicitud_estado", x => x.vecino_solicitud_estado_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_tipo_construccion",
                columns: table => new
                {
                    vecino_tipo_construccion_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_tipo_construccion", x => x.vecino_tipo_construccion_id);
                });

            migrationBuilder.CreateTable(
                name: "work_specialty",
                columns: table => new
                {
                    work_specialty_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_specialty_description = table.Column<string>(type: "text", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_specialty", x => x.work_specialty_id);
                });

            migrationBuilder.CreateTable(
                name: "workers_category",
                columns: table => new
                {
                    workers_category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workers_category", x => x.workers_category_id);
                });

            migrationBuilder.CreateTable(
                name: "ss_dossier_documento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dossier_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_doc = table.Column<string>(type: "text", nullable: false),
                    nombre_archivo = table.Column<string>(type: "text", nullable: true),
                    archivo_path = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_dossier_documento", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_dossier_documento_ss_dossier_semana_dossier_id",
                        column: x => x.dossier_id,
                        principalTable: "ss_dossier_semana",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_id = table.Column<int>(type: "integer", nullable: false),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    es_planificada = table.Column<bool>(type: "boolean", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    area = table.Column<string>(type: "text", nullable: true),
                    responsable_area = table.Column<string>(type: "text", nullable: true),
                    inspector_nombre = table.Column<string>(type: "text", nullable: true),
                    inspector_cargo = table.Column<string>(type: "text", nullable: true),
                    inspector_empresa = table.Column<string>(type: "text", nullable: true),
                    firma_inspector_url = table.Column<string>(type: "text", nullable: true),
                    representante_nombre = table.Column<string>(type: "text", nullable: true),
                    representante_cargo = table.Column<string>(type: "text", nullable: true),
                    firma_representante_url = table.Column<string>(type: "text", nullable: true),
                    descripcion_causas = table.Column<string>(type: "text", nullable: true),
                    conclusiones = table.Column<string>(type: "text", nullable: true),
                    total_items = table.Column<int>(type: "integer", nullable: false),
                    total_cumple = table.Column<int>(type: "integer", nullable: false),
                    total_no_cumple = table.Column<int>(type: "integer", nullable: false),
                    total_na = table.Column<int>(type: "integer", nullable: false),
                    tasa_cumplimiento = table.Column<decimal>(type: "numeric", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_contributor_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "contributor",
                        principalColumn: "contributor_id");
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_ssoma_inspeccion_tipo_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "ssoma_inspeccion_tipo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_checklist_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_id = table.Column<int>(type: "integer", nullable: false),
                    pregunta = table.Column<string>(type: "text", nullable: false),
                    categoria = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_checklist_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_checklist_item_ssoma_inspeccion_tipo_tipo_",
                        column: x => x.tipo_id,
                        principalTable: "ssoma_inspeccion_tipo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    pet_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_observacion = table.Column<string>(type: "text", nullable: false),
                    cuenta_con_pet = table.Column<bool>(type: "boolean", nullable: false),
                    area = table.Column<string>(type: "text", nullable: true),
                    se_informa_trabajador = table.Column<bool>(type: "boolean", nullable: false),
                    observador_nombre = table.Column<string>(type: "text", nullable: true),
                    observador_cargo = table.Column<string>(type: "text", nullable: true),
                    firma_observador_url = table.Column<string>(type: "text", nullable: true),
                    se_felicito = table.Column<bool>(type: "boolean", nullable: false),
                    se_recibieron_comentarios = table.Column<bool>(type: "boolean", nullable: false),
                    se_retroalimento = table.Column<bool>(type: "boolean", nullable: false),
                    se_obtuvo_compromiso = table.Column<bool>(type: "boolean", nullable: false),
                    accion_requerida = table.Column<string>(type: "text", nullable: true),
                    accion_observacion = table.Column<string>(type: "text", nullable: true),
                    total_pasos = table.Column<int>(type: "integer", nullable: false),
                    total_seguros = table.Column<int>(type: "integer", nullable: false),
                    total_inseguros = table.Column<int>(type: "integer", nullable: false),
                    score_pct = table.Column<decimal>(type: "numeric", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Completado"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_ssoma_pet_pet_id",
                        column: x => x.pet_id,
                        principalTable: "ssoma_pet",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ssoma_rac",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: false),
                    severidad = table.Column<string>(type: "text", nullable: false),
                    es_anonimo_reportante = table.Column<bool>(type: "boolean", nullable: false),
                    reportante_id = table.Column<int>(type: "integer", nullable: true),
                    reportante_nombre = table.Column<string>(type: "text", nullable: true),
                    reportante_cargo = table.Column<string>(type: "text", nullable: true),
                    empresa_reportante_id = table.Column<int>(type: "integer", nullable: true),
                    es_anonimo_observado = table.Column<bool>(type: "boolean", nullable: false),
                    observado_worker_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_reportada_id = table.Column<int>(type: "integer", nullable: true),
                    proyecto_piso = table.Column<string>(type: "text", nullable: true),
                    lugar_descripcion = table.Column<string>(type: "text", nullable: true),
                    latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    plan_accion = table.Column<string>(type: "text", nullable: true),
                    fecha_reporte = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    plazo_levantamiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Abierto"),
                    fecha_cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cierre_descripcion = table.Column<string>(type: "text", nullable: true),
                    cerrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    aplica_penalidad = table.Column<bool>(type: "boolean", nullable: false),
                    firma_reportante_url = table.Column<string>(type: "text", nullable: true),
                    firma_reportante_sp_id = table.Column<string>(type: "text", nullable: true),
                    pdf_url = table.Column<string>(type: "text", nullable: true),
                    pdf_sp_id = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_rac", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_rac_ssoma_rac_categoria_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "ssoma_rac_categoria",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino",
                columns: table => new
                {
                    vecino_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    predio = table.Column<string>(type: "text", nullable: true),
                    direccion = table.Column<string>(type: "text", nullable: false),
                    interior_departamento = table.Column<string>(type: "text", nullable: true),
                    nombre_propietario = table.Column<string>(type: "text", nullable: false),
                    dni = table.Column<string>(type: "text", nullable: false),
                    celular = table.Column<string>(type: "text", nullable: true),
                    vecino_colindancia_id = table.Column<int>(type: "integer", nullable: false),
                    vecino_tipo_construccion_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino", x => x.vecino_id);
                    table.ForeignKey(
                        name: "fk_vecino_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_vecino_colindancia_vecino_colindancia_id",
                        column: x => x.vecino_colindancia_id,
                        principalTable: "vecino_colindancia",
                        principalColumn: "vecino_colindancia_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_vecino_tipo_construccion_vecino_tipo_construccion_id",
                        column: x => x.vecino_tipo_construccion_id,
                        principalTable: "vecino_tipo_construccion",
                        principalColumn: "vecino_tipo_construccion_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_hallazgo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inspeccion_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    area = table.Column<string>(type: "text", nullable: true),
                    responsable_nombre = table.Column<string>(type: "text", nullable: true),
                    responsable_cargo = table.Column<string>(type: "text", nullable: true),
                    fecha_limite = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    accion_correctiva = table.Column<string>(type: "text", nullable: true),
                    evidencia_cierre_url = table.Column<string>(type: "text", nullable: true),
                    fecha_cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_hallazgo", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_hallazgo_ssoma_inspeccion_inspeccion_id",
                        column: x => x.inspeccion_id,
                        principalTable: "ssoma_inspeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_respuesta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inspeccion_id = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    resultado = table.Column<string>(type: "text", nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_respuesta", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_respuesta_ssoma_inspeccion_checklist_item_",
                        column: x => x.item_id,
                        principalTable: "ssoma_inspeccion_checklist_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_respuesta_ssoma_inspeccion_inspeccion_id",
                        column: x => x.inspeccion_id,
                        principalTable: "ssoma_inspeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt_paso",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opt_id = table.Column<int>(type: "integer", nullable: false),
                    numero_display = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    nivel = table.Column<int>(type: "integer", nullable: false),
                    resultado = table.Column<string>(type: "text", nullable: true),
                    desviacion_observada = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt_paso", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_paso_ssoma_opt_opt_id",
                        column: x => x.opt_id,
                        principalTable: "ssoma_opt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt_trabajador",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opt_id = table.Column<int>(type: "integer", nullable: false),
                    trabajador_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_trabajador = table.Column<string>(type: "text", nullable: true),
                    tiempo_en_obra = table.Column<string>(type: "text", nullable: true),
                    anios_experiencia = table.Column<string>(type: "text", nullable: true),
                    firma_trabajador_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt_trabajador", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_trabajador_ssoma_opt_opt_id",
                        column: x => x.opt_id,
                        principalTable: "ssoma_opt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_trabajador_workers_trabajador_id",
                        column: x => x.trabajador_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_opt_verificacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opt_id = table.Column<int>(type: "integer", nullable: false),
                    criterio_id = table.Column<int>(type: "integer", nullable: false),
                    resultado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_opt_verificacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_verificacion_ssoma_opt_criterio_verificacion_crit",
                        column: x => x.criterio_id,
                        principalTable: "ssoma_opt_criterio_verificacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ssoma_opt_verificacion_ssoma_opt_opt_id",
                        column: x => x.opt_id,
                        principalTable: "ssoma_opt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_rac_foto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rac_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    sp_id = table.Column<string>(type: "text", nullable: true),
                    tipo = table.Column<string>(type: "text", nullable: false, defaultValue: "Hallazgo"),
                    nombre_archivo = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    subido_por = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_rac_foto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_rac_foto_ssoma_rac_rac_id",
                        column: x => x.rac_id,
                        principalTable: "ssoma_rac",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_rac_penalidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    rac_id = table.Column<int>(type: "integer", nullable: false),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    infraccion_id = table.Column<int>(type: "integer", nullable: true),
                    monto_calculado = table.Column<decimal>(type: "numeric", nullable: false),
                    uit_referencia = table.Column<decimal>(type: "numeric", nullable: false),
                    descripcion_ocurrido = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false, defaultValue: "EnEvaluacion"),
                    descargo_texto = table.Column<string>(type: "text", nullable: true),
                    documento_url = table.Column<string>(type: "text", nullable: true),
                    descargo_fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    descargo_usuario_id = table.Column<int>(type: "integer", nullable: true),
                    resolucion_texto = table.Column<string>(type: "text", nullable: true),
                    resolucion_tipo = table.Column<string>(type: "text", nullable: true),
                    resuelto_por_id = table.Column<int>(type: "integer", nullable: true),
                    resuelta_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pdf_resolucion_url = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_rac_penalidad", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_rac_penalidad_ssoma_rac_infraccion_infraccion_id",
                        column: x => x.infraccion_id,
                        principalTable: "ssoma_rac_infraccion",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ssoma_rac_penalidad_ssoma_rac_rac_id",
                        column: x => x.rac_id,
                        principalTable: "ssoma_rac",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_solicitud",
                columns: table => new
                {
                    vecino_solicitud_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    es_critica = table.Column<bool>(type: "boolean", nullable: false),
                    vecino_solicitud_estado_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_solicitud", x => x.vecino_solicitud_id);
                    table.ForeignKey(
                        name: "fk_vecino_solicitud_vecino_solicitud_estado_vecino_solicitud_e",
                        column: x => x.vecino_solicitud_estado_id,
                        principalTable: "vecino_solicitud_estado",
                        principalColumn: "vecino_solicitud_estado_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_solicitud_vecino_vecino_id",
                        column: x => x.vecino_id,
                        principalTable: "vecino",
                        principalColumn: "vecino_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ssoma_inspeccion_hallazgo_foto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hallazgo_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ssoma_inspeccion_hallazgo_foto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ssoma_inspeccion_hallazgo_foto_ssoma_inspeccion_hallazgo_ha",
                        column: x => x.hallazgo_id,
                        principalTable: "ssoma_inspeccion_hallazgo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_compromiso",
                columns: table => new
                {
                    vecino_compromiso_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_solicitud_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    es_critico = table.Column<bool>(type: "boolean", nullable: false),
                    vecino_compromiso_estado_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_compromiso", x => x.vecino_compromiso_id);
                    table.ForeignKey(
                        name: "fk_vecino_compromiso_vecino_compromiso_estado_vecino_compromis",
                        column: x => x.vecino_compromiso_estado_id,
                        principalTable: "vecino_compromiso_estado",
                        principalColumn: "vecino_compromiso_estado_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_compromiso_vecino_solicitud_vecino_solicitud_id",
                        column: x => x.vecino_solicitud_id,
                        principalTable: "vecino_solicitud",
                        principalColumn: "vecino_solicitud_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_compromiso_entregable",
                columns: table => new
                {
                    vecino_compromiso_entregable_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_compromiso_id = table.Column<int>(type: "integer", nullable: false),
                    vecino_entregable_tipo_id = table.Column<int>(type: "integer", nullable: false),
                    vecino_entregable_estado_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_compromiso_entregable", x => x.vecino_compromiso_entregable_id);
                    table.ForeignKey(
                        name: "fk_vecino_compromiso_entregable_vecino_compromiso_vecino_compr",
                        column: x => x.vecino_compromiso_id,
                        principalTable: "vecino_compromiso",
                        principalColumn: "vecino_compromiso_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_compromiso_entregable_vecino_entregable_estado_vecin",
                        column: x => x.vecino_entregable_estado_id,
                        principalTable: "vecino_entregable_estado",
                        principalColumn: "vecino_entregable_estado_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_compromiso_entregable_vecino_entregable_tipo_vecino_",
                        column: x => x.vecino_entregable_tipo_id,
                        principalTable: "vecino_entregable_tipo",
                        principalColumn: "vecino_entregable_tipo_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_dossier_documento_dossier_id_tipo_doc",
                table: "ss_dossier_documento",
                columns: new[] { "dossier_id", "tipo_doc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ss_dossier_semana_contributor_id_proyecto_id_anio_numero_se",
                table: "ss_dossier_semana",
                columns: new[] { "contributor_id", "proyecto_id", "anio", "numero_semana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_empresa_id",
                table: "ssoma_inspeccion",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_proyecto_id",
                table: "ssoma_inspeccion",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_tipo_id",
                table: "ssoma_inspeccion",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_checklist_item_tipo_id",
                table: "ssoma_inspeccion_checklist_item",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_hallazgo_inspeccion_id",
                table: "ssoma_inspeccion_hallazgo",
                column: "inspeccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_hallazgo_foto_hallazgo_id",
                table: "ssoma_inspeccion_hallazgo_foto",
                column: "hallazgo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_respuesta_inspeccion_id",
                table: "ssoma_inspeccion_respuesta",
                column: "inspeccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_inspeccion_respuesta_item_id",
                table: "ssoma_inspeccion_respuesta",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_pet_id",
                table: "ssoma_opt",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_proyecto_id",
                table: "ssoma_opt",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_paso_opt_id",
                table: "ssoma_opt_paso",
                column: "opt_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_trabajador_opt_id",
                table: "ssoma_opt_trabajador",
                column: "opt_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_trabajador_trabajador_id",
                table: "ssoma_opt_trabajador",
                column: "trabajador_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_verificacion_criterio_id",
                table: "ssoma_opt_verificacion",
                column: "criterio_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_opt_verificacion_opt_id",
                table: "ssoma_opt_verificacion",
                column: "opt_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_paso_ejecucion_archivo_ejecucion_id",
                table: "ssoma_paso_ejecucion_archivo",
                column: "ejecucion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_rac_categoria_id",
                table: "ssoma_rac",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_rac_foto_rac_id",
                table: "ssoma_rac_foto",
                column: "rac_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_rac_penalidad_infraccion_id",
                table: "ssoma_rac_penalidad",
                column: "infraccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_ssoma_rac_penalidad_rac_id",
                table: "ssoma_rac_penalidad",
                column: "rac_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vecino_project_id",
                table: "vecino",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_vecino_colindancia_id",
                table: "vecino",
                column: "vecino_colindancia_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_vecino_tipo_construccion_id",
                table: "vecino",
                column: "vecino_tipo_construccion_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_compromiso_vecino_compromiso_estado_id",
                table: "vecino_compromiso",
                column: "vecino_compromiso_estado_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_compromiso_vecino_solicitud_id",
                table: "vecino_compromiso",
                column: "vecino_solicitud_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_compromiso_entregable_vecino_compromiso_id",
                table: "vecino_compromiso_entregable",
                column: "vecino_compromiso_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_compromiso_entregable_vecino_entregable_estado_id",
                table: "vecino_compromiso_entregable",
                column: "vecino_entregable_estado_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_compromiso_entregable_vecino_entregable_tipo_id",
                table: "vecino_compromiso_entregable",
                column: "vecino_entregable_tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_solicitud_vecino_id",
                table: "vecino_solicitud",
                column: "vecino_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_solicitud_vecino_solicitud_estado_id",
                table: "vecino_solicitud",
                column: "vecino_solicitud_estado_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "costos_cronograma");

            migrationBuilder.DropTable(
                name: "costos_cronograma_actividad");

            migrationBuilder.DropTable(
                name: "costos_cronograma_actividad_nodo");

            migrationBuilder.DropTable(
                name: "project_adjudicacion_folder");

            migrationBuilder.DropTable(
                name: "ss_dossier_documento");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_hallazgo_foto");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_respuesta");

            migrationBuilder.DropTable(
                name: "ssoma_opt_paso");

            migrationBuilder.DropTable(
                name: "ssoma_opt_trabajador");

            migrationBuilder.DropTable(
                name: "ssoma_opt_verificacion");

            migrationBuilder.DropTable(
                name: "ssoma_paso_ejecucion_archivo");

            migrationBuilder.DropTable(
                name: "ssoma_rac_foto");

            migrationBuilder.DropTable(
                name: "ssoma_rac_penalidad");

            migrationBuilder.DropTable(
                name: "ssoma_uit_anio");

            migrationBuilder.DropTable(
                name: "vecino_compromiso_entregable");

            migrationBuilder.DropTable(
                name: "work_specialty");

            migrationBuilder.DropTable(
                name: "workers_category");

            migrationBuilder.DropTable(
                name: "ss_dossier_semana");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_hallazgo");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_checklist_item");

            migrationBuilder.DropTable(
                name: "ssoma_opt_criterio_verificacion");

            migrationBuilder.DropTable(
                name: "ssoma_opt");

            migrationBuilder.DropTable(
                name: "ssoma_rac_infraccion");

            migrationBuilder.DropTable(
                name: "ssoma_rac");

            migrationBuilder.DropTable(
                name: "vecino_compromiso");

            migrationBuilder.DropTable(
                name: "vecino_entregable_estado");

            migrationBuilder.DropTable(
                name: "vecino_entregable_tipo");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion");

            migrationBuilder.DropTable(
                name: "ssoma_pet");

            migrationBuilder.DropTable(
                name: "ssoma_rac_categoria");

            migrationBuilder.DropTable(
                name: "vecino_compromiso_estado");

            migrationBuilder.DropTable(
                name: "vecino_solicitud");

            migrationBuilder.DropTable(
                name: "ssoma_inspeccion_tipo");

            migrationBuilder.DropTable(
                name: "vecino_solicitud_estado");

            migrationBuilder.DropTable(
                name: "vecino");

            migrationBuilder.DropTable(
                name: "vecino_colindancia");

            migrationBuilder.DropTable(
                name: "vecino_tipo_construccion");

            migrationBuilder.DropColumn(
                name: "anios_experiencia",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "worker_category_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "worker_lesson_jefe_id",
                table: "workers");

            migrationBuilder.DropColumn(
                name: "contract_modality_id",
                table: "work_item_category_clause");

            migrationBuilder.DropColumn(
                name: "work_specialty_id",
                table: "work_item");

            migrationBuilder.DropColumn(
                name: "worker_id",
                table: "user_project");

            migrationBuilder.DropColumn(
                name: "adjudicacion_folder_name",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "arrival_observation",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "includes_carta_fianza",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "payment_days",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "step6_signed_costos",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "step6_signed_gerente_general",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "step6_signed_gerente_inmobiliario",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "work_specialty_id",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "contador_penalidad",
                table: "project");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "user_project",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "anio",
                table: "ssoma_paso",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
