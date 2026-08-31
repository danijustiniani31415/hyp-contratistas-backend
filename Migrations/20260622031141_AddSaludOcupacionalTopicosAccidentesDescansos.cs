using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSaludOcupacionalTopicosAccidentesDescansos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "work_specialty_id",
                table: "work_item");

            migrationBuilder.AddColumn<bool>(
                name: "interconsulta_resuelta",
                table: "worker_emos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "archivo_url",
                table: "vecino_compromiso_entregable",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                table: "vecino_compromiso_entregable",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nombre_propietario",
                table: "vecino",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "dni",
                table: "vecino",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "observaciones",
                table: "vecino",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vecino_uso_id",
                table: "vecino",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modulos",
                table: "ss_contratista_usuario",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "worker_id",
                table: "ss_contratista_usuario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_work_item_name",
                table: "project_sub_contractor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_labor",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_subcontract",
                table: "project_sub_contractor",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "contractor_update_request",
                columns: table => new
                {
                    contractor_update_request_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contractor_id = table.Column<int>(type: "integer", nullable: false),
                    contractor_update_state_id = table.Column<int>(type: "integer", nullable: false),
                    contributor_ruc = table.Column<string>(type: "text", nullable: false),
                    contributor_name = table.Column<string>(type: "text", nullable: false),
                    contributor_address = table.Column<string>(type: "text", nullable: true),
                    contributor_economic_activity_description = table.Column<string>(type: "text", nullable: true),
                    contributor_district = table.Column<string>(type: "text", nullable: true),
                    contributor_province = table.Column<string>(type: "text", nullable: true),
                    contributor_department = table.Column<string>(type: "text", nullable: true),
                    legal_representative_dni = table.Column<string>(type: "text", nullable: true),
                    legal_representative_full_name = table.Column<string>(type: "text", nullable: true),
                    legal_entity_registry_number = table.Column<string>(type: "text", nullable: true),
                    logo_file_url = table.Column<string>(type: "text", nullable: true),
                    brochure_file_url = table.Column<string>(type: "text", nullable: true),
                    ficha_ruc_file_url = table.Column<string>(type: "text", nullable: true),
                    references_list_file_url = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_update_request", x => x.contractor_update_request_id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_update_state",
                columns: table => new
                {
                    contractor_update_state_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contractor_update_state_description = table.Column<string>(type: "text", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_update_state", x => x.contractor_update_state_id);
                });

            migrationBuilder.CreateTable(
                name: "project_croquis",
                columns: table => new
                {
                    project_croquis_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_croquis", x => x.project_croquis_id);
                    table.ForeignKey(
                        name: "fk_project_croquis_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_accidente_trabajo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_accidente = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_accidente = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    lugar_accidente = table.Column<string>(type: "text", nullable: true),
                    tipo_accidente = table.Column<string>(type: "text", nullable: true),
                    mecanismo = table.Column<string>(type: "text", nullable: true),
                    parte_cuerpo_afectada = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    descripcion_lesion = table.Column<string>(type: "text", nullable: true),
                    requiere_hospitalizacion = table.Column<bool>(type: "boolean", nullable: false),
                    hospital_nombre = table.Column<string>(type: "text", nullable: true),
                    atencion_topico_id = table.Column<int>(type: "integer", nullable: true),
                    dias_descanso_estimados = table.Column<int>(type: "integer", nullable: false),
                    dias_descanso_reales = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_alta = table.Column<DateOnly>(type: "date", nullable: true),
                    restricciones_reintegro = table.Column<string>(type: "text", nullable: true),
                    notificado_sunafil = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_notificacion_sunafil = table.Column<DateOnly>(type: "date", nullable: true),
                    numero_notificacion_sunafil = table.Column<string>(type: "text", nullable: true),
                    paso_id = table.Column<int>(type: "integer", nullable: true),
                    url_informe = table.Column<string>(type: "text", nullable: true),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    cerrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_accidente_trabajo", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_accidente_trabajo_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_caso_social",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_apertura = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_caso = table.Column<string>(type: "text", nullable: false),
                    prioridad = table.Column<string>(type: "text", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_cierre = table.Column<DateOnly>(type: "date", nullable: true),
                    resultado = table.Column<string>(type: "text", nullable: true),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    cerrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_caso_social", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_caso_social_contributor_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "contributor",
                        principalColumn: "contributor_id");
                    table.ForeignKey(
                        name: "fk_ss_caso_social_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "fk_ss_caso_social_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_charla_programa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creado_por_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_charla_programa", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_dossier_documento_archivo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    documento_id = table.Column<int>(type: "integer", nullable: false),
                    nombre_archivo = table.Column<string>(type: "text", nullable: false),
                    archivo_path = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_dossier_documento_archivo", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_dossier_documento_archivo_ss_dossier_documento_documento",
                        column: x => x.documento_id,
                        principalTable: "ss_dossier_documento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_hab_auditoria",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contractor_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_entregable_id = table.Column<int>(type: "integer", nullable: true),
                    worker_entregable_id = table.Column<int>(type: "integer", nullable: true),
                    accion = table.Column<string>(type: "text", nullable: false),
                    realizado_por_user_id = table.Column<int>(type: "integer", nullable: false),
                    realizado_por_nombre = table.Column<string>(type: "text", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    detalle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_hab_auditoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_topico_atencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    tipo_atencion_id = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    diagnostico = table.Column<string>(type: "text", nullable: true),
                    diagnostico_cie10 = table.Column<string>(type: "text", nullable: true),
                    tratamiento = table.Column<string>(type: "text", nullable: true),
                    medicamentos = table.Column<string>(type: "text", nullable: true),
                    presion_arterial = table.Column<string>(type: "text", nullable: true),
                    temperatura = table.Column<decimal>(type: "numeric", nullable: true),
                    frecuencia_cardiaca = table.Column<int>(type: "integer", nullable: true),
                    saturacion_oxigeno = table.Column<int>(type: "integer", nullable: true),
                    peso = table.Column<decimal>(type: "numeric", nullable: true),
                    derivado_clinica = table.Column<bool>(type: "boolean", nullable: false),
                    clinica_derivacion = table.Column<string>(type: "text", nullable: true),
                    genera_descanso = table.Column<bool>(type: "boolean", nullable: false),
                    descanso_dias = table.Column<int>(type: "integer", nullable: true),
                    genera_accidente = table.Column<bool>(type: "boolean", nullable: false),
                    accidente_id = table.Column<int>(type: "integer", nullable: true),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_topico_atencion", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_topico_atencion_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_topico_atencion_ss_topico_tipo_atencion_tipo_atencion_id",
                        column: x => x.tipo_atencion_id,
                        principalTable: "ss_topico_tipo_atencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ss_topico_tipo_atencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_topico_tipo_atencion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_imagen",
                columns: table => new
                {
                    vecino_imagen_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_id = table.Column<int>(type: "integer", nullable: false),
                    archivo_url = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_imagen", x => x.vecino_imagen_id);
                    table.ForeignKey(
                        name: "fk_vecino_imagen_vecino_vecino_id",
                        column: x => x.vecino_id,
                        principalTable: "vecino",
                        principalColumn: "vecino_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_relacion_tipo",
                columns: table => new
                {
                    vecino_relacion_tipo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_relacion_tipo", x => x.vecino_relacion_tipo_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_requisito_estado",
                columns: table => new
                {
                    vecino_requisito_estado_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_requisito_estado", x => x.vecino_requisito_estado_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_requisito_tipo",
                columns: table => new
                {
                    vecino_requisito_tipo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_requisito_tipo", x => x.vecino_requisito_tipo_id);
                });

            migrationBuilder.CreateTable(
                name: "vecino_uso",
                columns: table => new
                {
                    vecino_uso_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_uso", x => x.vecino_uso_id);
                });

            migrationBuilder.CreateTable(
                name: "work_item_valorization_form",
                columns: table => new
                {
                    work_item_valorization_form_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_item_id = table.Column<int>(type: "integer", nullable: false),
                    concept = table.Column<string>(type: "text", nullable: false),
                    percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    created_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_item_valorization_form", x => x.work_item_valorization_form_id);
                    table.ForeignKey(
                        name: "fk_work_item_valorization_form_work_item_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_item",
                        principalColumn: "work_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_update_request_email",
                columns: table => new
                {
                    contractor_update_request_email_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contractor_update_request_id = table.Column<int>(type: "integer", nullable: false),
                    contractor_email = table.Column<string>(type: "text", nullable: false),
                    contractor_person_type_id = table.Column<int>(type: "integer", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_update_request_email", x => x.contractor_update_request_email_id);
                    table.ForeignKey(
                        name: "fk_contractor_update_request_email_contractor_update_request_c",
                        column: x => x.contractor_update_request_id,
                        principalTable: "contractor_update_request",
                        principalColumn: "contractor_update_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_croquis_lote",
                columns: table => new
                {
                    project_croquis_lote_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_croquis_id = table.Column<int>(type: "integer", nullable: false),
                    numero_lote = table.Column<string>(type: "text", nullable: false),
                    poligono = table.Column<string>(type: "text", nullable: false),
                    vecino_id = table.Column<int>(type: "integer", nullable: true),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_croquis_lote", x => x.project_croquis_lote_id);
                    table.ForeignKey(
                        name: "fk_project_croquis_lote_project_croquis_project_croquis_id",
                        column: x => x.project_croquis_id,
                        principalTable: "project_croquis",
                        principalColumn: "project_croquis_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_accidente_seguimiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accidente_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    proxima_cita = table.Column<DateOnly>(type: "date", nullable: true),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_accidente_seguimiento", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_accidente_seguimiento_ss_accidente_trabajo_accidente_id",
                        column: x => x.accidente_id,
                        principalTable: "ss_accidente_trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_descanso_medico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    diagnostico = table.Column<string>(type: "text", nullable: true),
                    diagnostico_cie10 = table.Column<string>(type: "text", nullable: true),
                    medico_certifica = table.Column<string>(type: "text", nullable: true),
                    establecimiento = table.Column<string>(type: "text", nullable: true),
                    url_certificado = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                    aprobado_por_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_aprobacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accidente_id = table.Column<int>(type: "integer", nullable: true),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    empresa_id = table.Column<int>(type: "integer", nullable: true),
                    notificado_gth = table.Column<bool>(type: "boolean", nullable: false),
                    notificado_jefe = table.Column<bool>(type: "boolean", nullable: false),
                    reportado_por_trabajador = table.Column<bool>(type: "boolean", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    dias = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: true),
                    url_documento = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_descanso_medico", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_descanso_medico_ss_accidente_trabajo_accidente_id",
                        column: x => x.accidente_id,
                        principalTable: "ss_accidente_trabajo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ss_descanso_medico_workers_worker_id",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_caso_social_seguimiento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    caso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    responsable_id = table.Column<int>(type: "integer", nullable: true),
                    proxima_accion = table.Column<DateOnly>(type: "date", nullable: true),
                    accion_tomada = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_caso_social_seguimiento", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_caso_social_seguimiento_ss_caso_social_caso_id",
                        column: x => x.caso_id,
                        principalTable: "ss_caso_social",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_caso_social_seguimiento_workers_responsable_id",
                        column: x => x.responsable_id,
                        principalTable: "workers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ss_charla",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    programa_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    duracion_horas = table.Column<decimal>(type: "numeric", nullable: false),
                    supervisor_id = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    evidencia_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    evidencia_nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    evidencia_sp_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    evidencia_subida_por_id = table.Column<int>(type: "integer", nullable: true),
                    evidencia_subida_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    creado_por_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_charla", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_charla_ss_charla_programa_programa_id",
                        column: x => x.programa_id,
                        principalTable: "ss_charla_programa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_persona",
                columns: table => new
                {
                    vecino_persona_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    dni = table.Column<string>(type: "text", nullable: true),
                    celular = table.Column<string>(type: "text", nullable: true),
                    vecino_relacion_tipo_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_persona", x => x.vecino_persona_id);
                    table.ForeignKey(
                        name: "fk_vecino_persona_vecino_relacion_tipo_vecino_relacion_tipo_id",
                        column: x => x.vecino_relacion_tipo_id,
                        principalTable: "vecino_relacion_tipo",
                        principalColumn: "vecino_relacion_tipo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_persona_vecino_vecino_id",
                        column: x => x.vecino_id,
                        principalTable: "vecino",
                        principalColumn: "vecino_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vecino_requisito",
                columns: table => new
                {
                    vecino_requisito_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    vecino_id = table.Column<int>(type: "integer", nullable: false),
                    vecino_requisito_tipo_id = table.Column<int>(type: "integer", nullable: false),
                    vecino_requisito_estado_id = table.Column<int>(type: "integer", nullable: false),
                    archivo_url = table.Column<string>(type: "text", nullable: true),
                    original_file_name = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vecino_requisito", x => x.vecino_requisito_id);
                    table.ForeignKey(
                        name: "fk_vecino_requisito_vecino_requisito_estado_vecino_requisito_e",
                        column: x => x.vecino_requisito_estado_id,
                        principalTable: "vecino_requisito_estado",
                        principalColumn: "vecino_requisito_estado_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_requisito_vecino_requisito_tipo_vecino_requisito_tip",
                        column: x => x.vecino_requisito_tipo_id,
                        principalTable: "vecino_requisito_tipo",
                        principalColumn: "vecino_requisito_tipo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vecino_requisito_vecino_vecino_id",
                        column: x => x.vecino_id,
                        principalTable: "vecino",
                        principalColumn: "vecino_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_charla_asistencia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    charla_id = table.Column<int>(type: "integer", nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    asistio = table.Column<bool>(type: "boolean", nullable: false),
                    registrado_por_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_charla_asistencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_charla_asistencia_ss_charla_charla_id",
                        column: x => x.charla_id,
                        principalTable: "ss_charla",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vecino_vecino_uso_id",
                table: "vecino",
                column: "vecino_uso_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_update_request_email_contractor_update_request_id",
                table: "contractor_update_request_email",
                column: "contractor_update_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_croquis_project_id",
                table: "project_croquis",
                column: "project_id",
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_project_croquis_lote_project_croquis_id",
                table: "project_croquis_lote",
                column: "project_croquis_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_accidente_seguimiento_accidente_id",
                table: "ss_accidente_seguimiento",
                column: "accidente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_accidente_trabajo_worker_id",
                table: "ss_accidente_trabajo",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_caso_social_empresa_id",
                table: "ss_caso_social",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_caso_social_proyecto_id",
                table: "ss_caso_social",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_caso_social_worker_id",
                table: "ss_caso_social",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_caso_social_seguimiento_caso_id",
                table: "ss_caso_social_seguimiento",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_caso_social_seguimiento_responsable_id",
                table: "ss_caso_social_seguimiento",
                column: "responsable_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_charla_programa_id",
                table: "ss_charla",
                column: "programa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_charla_asistencia_charla_id",
                table: "ss_charla_asistencia",
                column: "charla_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_medico_accidente_id",
                table: "ss_descanso_medico",
                column: "accidente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_descanso_medico_worker_id",
                table: "ss_descanso_medico",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_dossier_documento_archivo_documento_id",
                table: "ss_dossier_documento_archivo",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_topico_atencion_worker_id",
                table: "ss_topico_atencion",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_topico_atencion_tipo_atencion_id",
                table: "ss_topico_atencion",
                column: "tipo_atencion_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_imagen_vecino_id",
                table: "vecino_imagen",
                column: "vecino_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_persona_vecino_id",
                table: "vecino_persona",
                column: "vecino_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_persona_vecino_relacion_tipo_id",
                table: "vecino_persona",
                column: "vecino_relacion_tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_requisito_vecino_id_vecino_requisito_tipo_id",
                table: "vecino_requisito",
                columns: new[] { "vecino_id", "vecino_requisito_tipo_id" },
                unique: true,
                filter: "state = true");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_requisito_vecino_requisito_estado_id",
                table: "vecino_requisito",
                column: "vecino_requisito_estado_id");

            migrationBuilder.CreateIndex(
                name: "ix_vecino_requisito_vecino_requisito_tipo_id",
                table: "vecino_requisito",
                column: "vecino_requisito_tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_item_valorization_form_work_item_id",
                table: "work_item_valorization_form",
                column: "work_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_vecino_vecino_uso_vecino_uso_id",
                table: "vecino",
                column: "vecino_uso_id",
                principalTable: "vecino_uso",
                principalColumn: "vecino_uso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vecino_vecino_uso_vecino_uso_id",
                table: "vecino");

            migrationBuilder.DropTable(
                name: "contractor_update_request_email");

            migrationBuilder.DropTable(
                name: "contractor_update_state");

            migrationBuilder.DropTable(
                name: "project_croquis_lote");

            migrationBuilder.DropTable(
                name: "ss_accidente_seguimiento");

            migrationBuilder.DropTable(
                name: "ss_caso_social_seguimiento");

            migrationBuilder.DropTable(
                name: "ss_charla_asistencia");

            migrationBuilder.DropTable(
                name: "ss_descanso_medico");

            migrationBuilder.DropTable(
                name: "ss_dossier_documento_archivo");

            migrationBuilder.DropTable(
                name: "ss_hab_auditoria");

            migrationBuilder.DropTable(
                name: "ss_topico_atencion");

            migrationBuilder.DropTable(
                name: "ss_topico_tipo_atencion");

            migrationBuilder.DropTable(
                name: "vecino_imagen");

            migrationBuilder.DropTable(
                name: "vecino_persona");

            migrationBuilder.DropTable(
                name: "vecino_requisito");

            migrationBuilder.DropTable(
                name: "vecino_uso");

            migrationBuilder.DropTable(
                name: "work_item_valorization_form");

            migrationBuilder.DropTable(
                name: "contractor_update_request");

            migrationBuilder.DropTable(
                name: "project_croquis");

            migrationBuilder.DropTable(
                name: "ss_caso_social");

            migrationBuilder.DropTable(
                name: "ss_charla");

            migrationBuilder.DropTable(
                name: "ss_accidente_trabajo");

            migrationBuilder.DropTable(
                name: "vecino_relacion_tipo");

            migrationBuilder.DropTable(
                name: "vecino_requisito_estado");

            migrationBuilder.DropTable(
                name: "vecino_requisito_tipo");

            migrationBuilder.DropTable(
                name: "ss_charla_programa");

            migrationBuilder.DropIndex(
                name: "ix_vecino_vecino_uso_id",
                table: "vecino");

            migrationBuilder.DropColumn(
                name: "interconsulta_resuelta",
                table: "worker_emos");

            migrationBuilder.DropColumn(
                name: "archivo_url",
                table: "vecino_compromiso_entregable");

            migrationBuilder.DropColumn(
                name: "original_file_name",
                table: "vecino_compromiso_entregable");

            migrationBuilder.DropColumn(
                name: "observaciones",
                table: "vecino");

            migrationBuilder.DropColumn(
                name: "vecino_uso_id",
                table: "vecino");

            migrationBuilder.DropColumn(
                name: "modulos",
                table: "ss_contratista_usuario");

            migrationBuilder.DropColumn(
                name: "worker_id",
                table: "ss_contratista_usuario");

            migrationBuilder.DropColumn(
                name: "contract_work_item_name",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "is_labor",
                table: "project_sub_contractor");

            migrationBuilder.DropColumn(
                name: "is_subcontract",
                table: "project_sub_contractor");

            migrationBuilder.AddColumn<int>(
                name: "work_specialty_id",
                table: "work_item",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nombre_propietario",
                table: "vecino",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dni",
                table: "vecino",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
