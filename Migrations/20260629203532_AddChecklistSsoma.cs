using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistSsoma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ss_accidente_trabajador
                DROP CONSTRAINT IF EXISTS fk_ss_accidente_trabajador_ss_accidente_incidente_accidente_in;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ss_accidente_trabajador
                DROP CONSTRAINT IF EXISTS pk_ss_accidente_trabajador;
                ALTER TABLE ss_accidente_trabajador
                DROP CONSTRAINT IF EXISTS ss_accidente_trabajador_pkey;
            ");

            migrationBuilder.RenameTable(
                name: "ss_accidente_trabajador",
                newName: "ssoma_accidente_trabajador");

            migrationBuilder.RenameIndex(
                name: "ix_ss_accidente_trabajador_accidente_incidente_id",
                table: "ssoma_accidente_trabajador",
                newName: "ix_ssoma_accidente_trabajador_accidente_incidente_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ssoma_accidente_trabajador",
                table: "ssoma_accidente_trabajador",
                column: "id");

            migrationBuilder.CreateTable(
                name: "ss_checklist_plantilla",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    tipo_activacion = table.Column<string>(type: "text", nullable: false),
                    evento_activacion = table.Column<string>(type: "text", nullable: true),
                    es_obligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_checklist_plantilla", x => x.id);
                });

            // ssoma_escuelita y ssoma_inhabilitacion ya existen en la BD (creadas por AddAmonestaciones)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ssoma_escuelita (
                    id serial PRIMARY KEY,
                    worker_id integer NOT NULL,
                    fecha date NOT NULL,
                    puntos_descontados integer NOT NULL,
                    observaciones text,
                    registrado_por integer,
                    created_at timestamptz NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_inhabilitacion (
                    id serial PRIMARY KEY,
                    worker_id integer NOT NULL,
                    tipo text NOT NULL,
                    motivo text,
                    puntos_al_momento integer,
                    fecha_inicio timestamptz NOT NULL,
                    fecha_fin timestamptz,
                    desbloqueado_por integer,
                    escuelita_id integer,
                    created_at timestamptz NOT NULL
                );
            ");

            migrationBuilder.CreateTable(
                name: "ss_checklist_plantilla_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plantilla_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    tiene_adjunto_ref = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_checklist_plantilla_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_checklist_plantilla_item_ss_checklist_plantilla_plantill",
                        column: x => x.plantilla_id,
                        principalTable: "ss_checklist_plantilla",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_checklist_proyecto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    plantilla_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    porcentaje_completado = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_activacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activado_por_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_completado = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notificacion_enviada = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_checklist_proyecto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_checklist_proyecto_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_checklist_proyecto_ss_checklist_plantilla_plantilla_id",
                        column: x => x.plantilla_id,
                        principalTable: "ss_checklist_plantilla",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_checklist_proyecto_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    checklist_proyecto_id = table.Column<int>(type: "integer", nullable: false),
                    plantilla_item_id = table.Column<int>(type: "integer", nullable: false),
                    completado = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_completado = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completado_por_id = table.Column<int>(type: "integer", nullable: true),
                    observacion = table.Column<string>(type: "text", nullable: true),
                    url_adjunto = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_checklist_proyecto_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_checklist_proyecto_item_ss_checklist_plantilla_item_plan",
                        column: x => x.plantilla_item_id,
                        principalTable: "ss_checklist_plantilla_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_checklist_proyecto_item_ss_checklist_proyecto_checklist_",
                        column: x => x.checklist_proyecto_id,
                        principalTable: "ss_checklist_proyecto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_checklist_proyecto_item_user_completado_por_id",
                        column: x => x.completado_por_id,
                        principalTable: "app_user",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_plantilla_item_plantilla_id",
                table: "ss_checklist_plantilla_item",
                column: "plantilla_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_plantilla_id",
                table: "ss_checklist_proyecto",
                column: "plantilla_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_proyecto_id",
                table: "ss_checklist_proyecto",
                column: "proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_item_checklist_proyecto_id",
                table: "ss_checklist_proyecto_item",
                column: "checklist_proyecto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_item_completado_por_id",
                table: "ss_checklist_proyecto_item",
                column: "completado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_checklist_proyecto_item_plantilla_item_id",
                table: "ss_checklist_proyecto_item",
                column: "plantilla_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ssoma_accidente_trabajador_ssoma_accidente_incidente_accide",
                table: "ssoma_accidente_trabajador",
                column: "accidente_incidente_id",
                principalTable: "ss_accidente_incidente",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // ── SEED: Plantillas ─────────────────────────────────────────────
            var now = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero);
            var cols = new[] { "nombre", "descripcion", "tipo_activacion", "evento_activacion", "es_obligatorio", "orden", "activo", "created_at" };
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Inicio de Proyecto",                       "Checklist obligatorio al inicio de cada proyecto",           "automatico", null,           true,  1,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Inicio de Demolición",                     "Checklist obligatorio antes de iniciar demolición",         "automatico", null,           true,  2,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Torre Grúa",                               "Requisitos para montaje y operación de torre grúa",         "manual",     "TORRE_GRUA",   false, 3,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Placing Boom",                             "Requisitos para uso de placing boom",                       "manual",     "PLACING_BOOM", false, 4,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Grúa Móvil",                               "Requisitos para operación de grúa móvil",                   "manual",     "GRUA_MOVIL",   false, 5,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Estudio de Suelo (Calicata)",              "Documentación para estudio de suelo con calicata",          "manual",     "CALICATA",     false, 6,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Habilitación Sala de Ventas",              "Requisitos para habilitar sala de ventas en obra",          "manual",     "SALA_VENTAS",  false, 7,  true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "SUNAFIL",                                  "Guía de preparación ante visita de SUNAFIL",                "manual",     null,           false, 10, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "ITSE",                                     "Inspección Técnica de Seguridad en Edificaciones",          "manual",     null,           false, 11, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "ITSE Oficina Central",                     "ITSE para oficina central",                                 "manual",     null,           false, 12, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Municipalidad",                            "Requisitos ante visita de la Municipalidad",                "manual",     null,           false, 13, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Auditoría MINTRA",                         "Auditoría Ley 29783 - D.S.005-2012-TR",                    "manual",     null,           false, 14, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Señales de Seguridad (Planos aprobados)",  "Guía de instalación de señalética según planos aprobados", "manual",     null,           false, 20, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Estación de Primeros Auxilios",            "Inventario de insumos de la estación de primeros auxilios", "manual",     null,           false, 21, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Seguridad de Información de SO",           "Controles de seguridad de información del área de SO",      "manual",     null,           false, 22, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Monitoreo de Medio Ambiente",              "Monitoreo de calidad de aire, ruido y meteorología",        "manual",     null,           false, 23, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Monitoreo Ocupacional",                    "Monitoreo de agentes físicos, químicos y biológicos",       "manual",     null,           false, 24, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla", cols, new object[] { "Declaración RS en Plataforma SINGERSOL",   "Registro de razón social en plataforma SINGERSOL",         "manual",     null,           false, 25, true, now });

            // ── SEED: Items ──────────────────────────────────────────────────
            var ic = new[] { "plantilla_id", "descripcion", "orden", "tiene_adjunto_ref", "activo", "created_at" };

            // 1 = Inicio de Proyecto
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Revisar la normativa local, ordenanzas municipales, del distrito donde está ubicado el proyecto", 1, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar si se cuenta con licencia de obra, permiso de interferencia de vías para carga y descarga y uso de media vereda para el cerco perimétrico", 2, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Solicitar el letrero informativo de obra de acuerdo a los datos de la licencia", 3, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Solicitar servicios higiénicos con lavatorio y duchas portátiles", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Coordinar el ingreso de personal PDR, vigía y monitor de obra", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar el Plan de Seguridad y Salud en el Trabajo y elementos", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Solicitar la inscripción en RENOCC de la obra", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Coordinar el ingreso de las concesionarias de alimentos", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Implementar el panel informativo de obra", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Implementar la estación de emergencia", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Implementar los cilindros de colores para los residuos", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar el Plan de Respuesta ante Emergencias", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar Plan para la Vigilancia, Prevención y Control Covid-19 de los trabajadores", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Coordinar la implementación del comedor", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Coordinar la implementación de los vestuarios", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar y publicar el mapa de riesgo", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar la implementación del Comité de Seguridad y Salud en el Trabajo", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar Inducción SSOMA al personal nuevo que ingresa a obra", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Validar la documentación de subcontratista nuevo que ingresa a obra", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Pegar el letrero de licencia de obra en el frontis", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Elaborar y publicar el IPERC de Línea Base", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar las protecciones colectivas", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar los puntales según necesidad y realizar el pedido con tiempo", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Solicitar insumos para señalización y delimitación de áreas", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Plano de protecciones colectivas", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Conformación de brigadas de emergencia", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Publicación de política SST", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar y publicar plano de evacuación", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Enviar requerimiento de extintores con sus bases", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Realizar implementación de chalecos de visitantes y cascos", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Definir puntos de lavado de manos", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Definir punto de hidratación", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Definir dónde se ubicarán los cilindros para gestión de residuos sólidos", 33, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Implementación de almacén MATPEL", 34, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Plan de Medio Ambiente", 35, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Implementar SERVICIOS DEL ÁREA BIENESTAR", 36, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Pozo a tierra en cada predio que se tenga licencia", 37, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Tablero eléctrico provisional estandarizado", 38, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar ubicación de torre grúa para que no exponga a caseta de ventas u otros", 39, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar que se tenga un presupuesto inicial real en lo referente a SSOMA", 40, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Verificar que las instalaciones eléctricas, agua y gas estén debidamente anuladas", 41, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 1, "Identificación con brazaletes para Brigadistas de Emergencias y Comité de Seguridad", 42, false, true, now });

            // 2 = Inicio de Demolición
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Revisar la normativa local, ordenanzas municipales, del distrito donde está ubicado el proyecto", 1, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar requerimiento inicial de señalización interna, EPPs, conos, barras retráctiles, malla raschel para cerco perimétrico, elementos para el botiquín y estación de emergencia, formatos de SST, segregación de residuos sólidos, panel informativo, gestión de visitas y otros", 2, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Solicitar el letrero informativo de obra de acuerdo a los datos de la licencia de obra DEMOLICIÓN", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Solicitar servicios higiénicos con lavatorio y duchas portátiles", 4, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Coordinar el ingreso de personal PDR, vigía y monitor de obra", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar el Plan de Seguridad y Salud en el Trabajo y elementos", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Solicitar la inscripción en RENOCC de la obra", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Coordinar el ingreso de las concesionarias de alimentos", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Implementar la estación de emergencia", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Implementar los cilindros de colores para los residuos", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar el Plan de Respuesta ante Emergencias", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar Plan para la Vigilancia, Prevención y Control Covid-19 de los trabajadores", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Coordinar la implementación del comedor", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Coordinar la implementación de los vestuarios", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar y publicar el mapa de riesgo", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar la implementación del Comité de Seguridad y Salud en el Trabajo", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar Inducción SSOMA al personal nuevo que ingresa a obra", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Validar la documentación de subcontratista nuevo que ingresa a obra", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Pegar el letrero de licencia de obra en el frontis", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Elaborar y publicar el IPERC de Línea Base", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificar las protecciones colectivas", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificar los puntales según necesidad y realizar el pedido con tiempo", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Solicitar insumos para señalización y delimitación de áreas", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Plano de protecciones colectivas", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Conformación de brigadas de emergencia", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar y publicar plano de evacuación", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Enviar requerimiento de extintores con sus bases", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar implementación de chalecos de visitantes y cascos", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Definir puntos de lavado de manos", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Definir punto de hidratación", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Definir dónde se ubicarán los cilindros para gestión de residuos sólidos", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Implementación de almacén MATPEL", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Plan de Medio Ambiente", 33, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Implementar SERVICIOS DEL ÁREA BIENESTAR", 34, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Pozo a tierra en cada predio que se tenga licencia", 35, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Tablero eléctrico provisional estandarizado", 36, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Maletín de primeros auxilios según D.S.011", 37, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificar y coordinar la colocación de protecciones colectivas para los vecinos con anticipación", 38, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificar la autorización para la ejecución de obra - Municipalidad", 39, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificar que la manguera de la cisterna no presente fisuras al humedecer el área", 40, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar requerimiento inicial de letreros de señalización vertical de acuerdo al plano de desvío del permiso de interferencia de vías", 41, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Aterramiento de la estructura metálica (cerco perimétrico) según normativa de protección eléctrica", 42, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Instalación de canaleta pasacables tipo rampa para control de interferencias en vereda interferida", 43, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Señalización luminosa mediante toletes en maniobra nocturna", 44, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Monitoreo aéreo preventivo mediante dron para evaluación de zonas críticas", 45, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Verificación de líneas energizadas mediante multímetro tipo pinza", 46, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Ejecución de calicatas técnicas como parte del análisis previo de riesgos", 47, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Utilización de bomba de presión de agua para control de emisiones particuladas", 48, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 2, "Realizar la toma de evidencia fotográfica de los vecinos con el dron del área de Marketing", 49, false, true, now });

            // 3 = Torre Grúa
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Certificado de Instalación y Operatividad", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Memoria de cálculo para zapata de Torre Grúa", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Manual de operación", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Procedimiento seguro de Montaje de Torre Grúa", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Plan y programa de Mantenimiento de la Torre Grúa", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Póliza TREC", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Póliza CAR", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Certificado del Operador de la Torre Grúa", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Certificado del Rigger", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "CV documentados de los técnicos de montaje", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Seguro SCTR y Vida Ley", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "EMO y Test de Altura", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Matriz de comunicaciones", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Matriz IPERC", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "PETS – Trabajos en altura", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Plan de emergencia", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Tabla de carga", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Certificación externa de Torre Grúa por SGS PERÚ (Plazo: 1er mes desde montaje)", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 3, "Plan rigging", 19, false, true, now });

            // 4 = Placing Boom
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 4, "Certificado de Operatividad de la Placing Boom", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 4, "Póliza TREC", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 4, "Certificado de Mantenimiento", 3, false, true, now });

            // 5 = Grúa Móvil
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Ficha técnica de la Grúa Móvil", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Certificado de Operatividad", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Certificado de equipo de izaje (eslingas, grilletes, cables)", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Manual de operación", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Póliza TREC", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Póliza CAR", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Certificado del Operador de la Grúa Móvil", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Certificado del Rigger", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Seguro SCTR y Vida Ley", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "EMO y Test de Altura", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Matriz de comunicaciones", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Matriz IPERC", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "PETS – Trabajos en altura", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Plan de emergencia", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Tabla de carga", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Cartilla de mantenimiento de maquinaria", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 5, "Radios de comunicación portátil (Operador de la Grúa Móvil y Rigger)", 17, false, true, now });

            // 6 = Estudio de Suelo
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "EMO", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Curriculum Vitae - CV", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Seguro Complementario de Trabajo de Riesgo - SCTR", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Póliza de Vida Ley", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Factura de pago SCTR y Vida Ley", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Hoja de asistencia médica de SCTR firmada y sellada por Gte. Gral.", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Plan SSOMA", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Plan de rescate", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Procedimiento Escrito de Trabajo Seguro", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "IPERC", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 6, "Supervisor de Campo", 11, false, true, now });

            // 7 = Habilitación Sala de Ventas
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Permisos de la Municipalidad", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Permiso de vías", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Protocolo pozo a tierra", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Tablero provisional completo", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Extintores vigentes con certificados", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Vestuarios", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Oficina técnica", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "SSHH", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Almacén", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 7, "Señalización de vías y ambientes", 10, false, true, now });

            // 8 = SUNAFIL
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Accesos libres, rutas de evacuación, señalización y protecciones colectivas", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Índice de accidentabilidad", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Kardex de entrega de EPPs y el cumplimiento de estas en campo", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Condiciones de seguridad", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Servicios de bienestar de acuerdo a la norma G050 (baños de staff y damas son adicionales)", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Las rampas de acceso deben contar con barandas de protección colectiva", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "El comedor debe estar limpio", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Los casilleros deben estar de acuerdo a la cantidad de trabajadores y en buen estado", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Las rutas de evacuación deben estar libres", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Los trabajadores en campo deben contar con todos sus EPPs y en buen estado", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "La obra debe contar con señalización de obligación, evacuación, advertencia y prohibición", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Orden y limpieza general de obra", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Barandas de protección en el perímetro de la obra", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Ductos cerrados", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Tranquera en Piso 1, del área del elevador", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Tableros eléctricos conectados a la línea de tierra, con diagrama unifilar y diferenciales", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Instalación de rodapié en zonas donde existan trabajos", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Orden y limpieza en área del comedor y vestuarios", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Verificación del TR5 y registro del personal en charla y hojas de asistencia al momento de la visita", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Certificados de operatividad de extintores", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "SCTR y Vida Ley del personal asistente el día de la visita", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Certificado del pozo a tierra", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Carnet RETCC del personal asistente el día de la visita", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Facturas y recibos de pago de los SCTR y Vida Ley", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Verificación de duchas, urinarios, lavatorios e inodoros según cantidad de personal, limpieza y distribución por género", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "El acceso a la prelosa debe realizarse mediante escaleras de andamio o similares (no telescópicas)", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Mantener orden y limpieza en vestuarios, casilleros correctamente rotulados y sin alambres como seguro", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Se deben colocar tapas o rodapiés en los ductos con barandas", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Colocar tapas a las canaletas de desagüe en las rampas de los sótanos", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Los cables de las oficinas deben estar correctamente instalados y tomacorrientes fijados", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Plano de protecciones colectivas de toda la obra", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Relación de todos los trabajadores con: Nombres, DNI, Fecha Ingreso, Cargo, Razón Social", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Contrato de las empresas contratistas indicando razón social, RUC y trabajos a realizar", 33, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Relación de todos los contratistas: Razón Social, RUC, domicilio fiscal y teléfonos", 34, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Licencia de construcción de la obra", 35, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Registro de charla de seguridad", 36, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "AST del día anterior y del día presente", 37, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Plan de Seguridad y Salud en el Trabajo", 38, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 8, "Certificado de operatividad de maquinaria", 39, false, true, now });

            // 9–18: ITSE, ITSE Oficina Central, Municipalidad, Auditoría MINTRA, Señales, Primeros Auxilios, Seg. Info SO, Monitoreo MA, Monitoreo Ocup., SINGERSOL
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE LUZ DE EMERGENCIA", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE RIESGO ELÉCTRICO", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE ZONA SEGURA DE SISMO", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE DIRECCIÓN DE SALIDA", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE SALIDA", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA DE SERVICIO HIGIÉNICO", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA PROHIBIDO EL INGRESO", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA BOTIQUÍN", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA EXTINTOR", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA NO FUMAR", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA ATENCIÓN PREFERENCIAL", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALÉTICA NO DISCRIMINACIÓN", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIFICADO DE LUZ DE EMERGENCIA", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "FUNCIONAMIENTO DE LUZ DE EMERGENCIA", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "LUZ DE EMERGENCIA CERCA A TABLERO ELÉCTRICO", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "GABINETE ELÉCTRICO DE METAL", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "TABLERO ELÉCTRICO CON MANDIL", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "LLAVES TÉRMICAS CORRESPONDE AL CABLEADO", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "DIFERENCIAL POR LLAVE TÉRMICA", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "TOMAS DE CORRIENTE CON ESPIGA A TIERRA", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "TABLERO Y LLAVES ROTULADAS", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "RESERVA DE TABLERO CON TAPAS", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CABLE A TIERRA DE COLOR AMARILLO", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "NO SE USA CABLES MELLIZOS", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "PROTOCOLO POZO A TIERRA", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIFICADO DE CALIBRACIÓN", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SEÑALIZACIÓN POZO A TIERRA", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CANALIZACIÓN DE CABLEADO", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIFICADO DE EXTINTORES", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "EXTINTOR PQS", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "EXTINTOR CO2", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "TARJETA DE INSPECCIÓN", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "GABINETE O PODIO EXTINTOR", 33, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERT. LÁMINA ESPEJO", 34, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERT. LÁMINA LUNAS", 35, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIF. FUNC. Y ATERRADO DE A/C", 36, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ATERRADO DE SPLITER", 37, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ATERRADO CONDENSADOR Y CARCASA", 38, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ESTRUCTURA SOPORTE SIN ÓXIDO", 39, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CONTENEDOR SIN ÓXIDO", 40, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ESTRUCTURA SIN DAÑOS (HUMEDAD, RAJADURA, ETC.)", 41, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ESTRUCTURA CONECTADA A TIERRA", 42, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "Rampa antiderrapante", 43, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "PASAMANO ESTABLES", 44, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIFICADO DE FUMIGACIÓN", 45, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "LICENCIA DE PUBLICIDAD", 46, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CARTA DE GARANTÍA DE CONTENEDOR", 47, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "CERTIFICADO DE ESCUADRAS", 48, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "PUERTAS ABREN EN SENTIDO DE CIRCULACIÓN", 49, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ATERRADO DE RACK DE COMUNICACIÓN", 50, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "ATERRADO DE ESTRUCTURA METÁLICA (RETIRO DE SALA)", 51, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "SALAS < 250 MTS: PULSADOR / > 250 MTS: SISTEMA ACI", 52, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "VIDRIOS PAVONADOS", 53, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 9, "LICENCIA DE FUNCIONAMIENTO (EXHIBIDA)", 54, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "LICENCIA DE FUNCIONAMIENTO (EXHIBIDA)", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE AFORO", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE LUZ DE EMERGENCIA", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE RIESGO ELÉCTRICO", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE ALARMA CONTRAINCENDIO", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE DIRECCIÓN DE SALIDA", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE SALIDA", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE SERVICIO HIGIÉNICO", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA PROHIBIDO EL INGRESO", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA NO FUMAR", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA EXTINTORES", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA ATENCIÓN PREFERENCIAL", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA NO DISCRIMINACIÓN", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALÉTICA DE USO DE ARNÉS (TECHO TÉCNICO)", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "DECLARACIÓN JURADA DE USO DE ARNÉS", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIFICADO DE LUZ DE EMERGENCIA", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "FUNCIONAMIENTO DE LUZ DE EMERGENCIA", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "LUZ DE EMERGENCIA CERCA A TABLERO ELÉCTRICO", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "GABINETE ELÉCTRICO DE METAL", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TABLERO ELÉCTRICO CON MANDIL", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "LLAVES TÉRMICAS CORRESPONDE AL CABLEADO", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "DIFERENCIAL POR LLAVE TÉRMICA", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TOMAS DE CORRIENTE CON ESPIGA A TIERRA", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TABLERO Y LLAVES ROTULADAS", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "DIRECTORIO Y DIAGRAMA UNIFILAR ACTUALIZADO DE CADA TABLERO", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "RESERVA DE TABLERO CON TAPAS", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CABLE A TIERRA DE COLOR AMARILLO DE TABLEROS", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "NO SE USA CABLES MELLIZOS", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PROTOCOLO POZO A TIERRA", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIFICADO DE CALIBRACIÓN", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "SEÑALIZACIÓN POZO A TIERRA", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CANALIZACIÓN DE CABLEADO", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIFICADO DE EXTINTORES", 33, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "EXTINTOR PQS (recargados y enumeración correlativa)", 34, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "EXTINTOR CO2 (recargados y enumeración correlativa)", 35, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TARJETA DE INSPECCIÓN", 36, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "GABINETE O PODIO EXTINTOR EN BUEN ESTADO, SIN ÓXIDO", 37, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERT. LÁMINA ESPEJO", 38, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERT. LÁMINA VIDRIOS", 39, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIF. FUNC. Y ATERRADO DE A/C (AIRE ACONDICIONADO)", 40, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ATERRADO DE SPLITER", 41, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ATERRADO CONDENSADOR Y CARCASA (cable amarillo visible)", 42, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ESTRUCTURA SOPORTE SIN ÓXIDO (realizar mantenimiento)", 43, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ESTRUCTURA SIN DAÑOS (HUMEDAD, RAJADURA, ETC.)", 44, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ESTRUCTURA CONECTADA A TIERRA (cable amarillo visible)", 45, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIF. DE MANTENIMIENTO DE GRUPO ELECTRÓGENO (cable de aterrado visible)", 46, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "GRUPO ELECTRÓGENO SIN ÓXIDO (PINTURA EN BUEN ESTADO)", 47, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CABLES CUBIERTOS CON TUBO CORRUGADO", 48, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TOMAS DE BAÑOS CON HIDROBOX", 49, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ATERRAMIENTO DE MESAS DE TRABAJO Y ESCRITORIOS (colocar sticker visible)", 50, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "RETIRAR EXTENSIONES EXTERNAS PROVISIONALES", 51, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ATERRAMIENTO DE SERVIDORES Y RACKS DE DATA (CABLE AMARILLO VISIBLE)", 52, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "ALMACENES ORDENADOS Y CON RUTAS DESPEJADAS", 53, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PASILLOS DESPEJADOS DE MATERIALES", 54, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PINTURA DE EDIFICIO SIN MOHO, RAJADURAS, ETC.", 55, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PASAMANO ESTABLES", 56, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIF. DE FUMIGACIÓN", 57, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CERTIFICADO DE ALARMA CONTRA INCENDIOS", 58, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "BARANDAS DE TECHO TÉCNICO SIN ÓXIDO, PINTURA EN BUEN ESTADO", 59, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PLAN DE SEGURIDAD ACTUALIZADO (AFORO, TIEMPO DE EVACUACIÓN, ACTA DE CAPACITACIONES)", 60, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "CRONOGRAMA DE CAPACITACIONES Y SIMULACROS", 61, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "DECLARACIÓN JURADA DE USO DE ARNÉS EN TECHO TÉCNICO y/o PETS del proveedor", 62, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "BRIGADA DE SEGURIDAD ACTUALIZADA", 63, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "TODAS LAS SEÑALÉTICAS FOTOLUMINISCENTES", 64, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PLANO UNIFILAR ACTUALIZADO CON LAS REMODELACIONES", 65, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PLANO INDECI ACTUALIZADO", 66, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "PLANO DE UBICACIÓN ACTUALIZADO", 67, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "DOCUMENTACIÓN EN FÍSICO PARA PASAR POR MESA DE PARTES SI LO SOLICITAN", 68, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 10, "REVISIÓN DE ENCHUFES (DEBEN SER DE TRES ESPIGAS)", 69, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Registro de Charla diaria", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "SCTR vigente para verificación", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Registro del último mantenimiento de la torre grúa", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Certificado de Operatividad de bobcat", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Certificado de Operador de bobcat", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "ATS y permisos de trabajos de riesgo del día", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Certificado de Operatividad de la Placing Boom", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Procedimiento de trabajo seguro de las actividades realizadas", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Certificado de disposición final de residuos sólidos", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Servicios de bienestar de acuerdo a la G050", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Protecciones colectivas", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Uso de EPP en campo", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Póliza CAR", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Permiso de interferencia de vías", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Señalización y rutas de acceso", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Pozo a tierra", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Línea de vida vertical independiente en el uso de andamios", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Inspección de extintor", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Tableros eléctricos: sin conexiones en paralelo, amperaje correcto, diagrama unifilar, luminarias y tomacorrientes independientes", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Plano de las mallas anticaídas", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Cálculo de memoria de las mallas anticaída", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Certificado de las mallas anticaída", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Protocolo del Pozo a Tierra", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Plan de Emergencia Actualizado", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Plan de Seguridad y Salud en el Trabajo", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Plan de Manejo Ambiental", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Constancia de botadero autorizado", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 11, "Memoria ambiental de operación de seguridad vial y medidas de mitigación ambiental actualizado", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Política SST (Registro de Difusión y entrega al personal)", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Conformación del Comité SST (Publicación del Organigrama en Paneles Informativos)", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Objetivos, plan y programa anual de SST (Difusión y entrega al personal)", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Reglamento Interno de SST (Registro de Entrega y Difusión: INDUCCIÓN)", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Matriz IPERC (Publicación en frente de trabajo firmado por SSOMA, registro difusión: INDUCCIÓN)", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Investigación y reporte de accidentes, enfermedades ocupacionales, incidentes peligrosos (Power Apps)", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Estadísticas de Accidentabilidad (Power Apps)", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Monitoreos ocupacionales: químicos, físicos, biológicos, disergonómicos, psicosociales, entre otros", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Planes y respuesta a emergencias, simulacros (Registro de Difusión y entrega al personal)", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Inspecciones planeadas y no planeadas (Power Apps)", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Comunicaciones, estadísticas, señalética (Power Apps)", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Inducción y capacitación (Power Apps e INDUCCIÓN)", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Gestión de contratistas en SST", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Auditorías internas y externas en SST", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Gestión del cambio", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Relación de trabajadores y sus puestos de trabajo (legajos, Perfiles de puesto o MOF)", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Informe de estadísticas trimestrales del Comité SST y resumen anual a la Gerencia General", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Mapa de riesgos (con servicios de bienestar: hidratación, estación emergencia, panel informativo, baños)", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Conformación de Brigadas de emergencias (Capacitaciones y lista de personal)", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "Evidencia de vigilancia del sistema de gestión de SST y revisión por la dirección", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 12, "CHECK LIST DE AUDITORÍA MINTRA LEY 29783, D.S.005-2012-TR", 21, true, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Solo colocar las señales que figuran en el plano aprobado, no descartar ninguna ni adicionar otra", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Todas las señales serán colocadas a la altura de 1.80 m", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Cuando se detecte un extintor en zona inaccesible, reportarlo al área de proyectos para compatibilizar planos", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Una vez culminada la colocación de señales, reportarlo al área de proyectos para revisión anticipada", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Al hacer el metrado de señales, no considerar las que se encuentran dentro de los departamentos", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Tener en cuenta el tipo de señales: fotoluminiscentes o luminosas (consultar al área de proyectos)", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Considerar los protectores de extintores para los que se encuentran a la intemperie", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Considerar un botiquín de primeros auxilios con insumos básicos entregado al conserje del lobby", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Consultar si la señal de pase de manguera se coloca en todas las caras o solo donde está la conexión de bomberos", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Al hacer requerimiento de extintores, también solicitar los ganchos tipo 'L', pernos y tarugos", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Para el requerimiento de extintores, considerar PQS de 6 kg (consultar con proyectos)", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Consultar a Jefatura de Calidad a qué distancia del suelo (1.5m o 1.2m) instalar el gancho tipo 'L'", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Antes de instalar el gancho tipo 'L', verificar que no haya tomacorrientes en la parte interior de la pared", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Antes de instalar el gancho tipo 'L', verificar que no haya conexiones ni ventas de extracción en la parte superior", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Consultar si el extintor CO2 del lobby irá en pedestal o con gancho tipo 'L'", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Antes de instalar señales en áreas comunes, consultar plano de detalles para evitar cruce con decoraciones", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Verificar que la distancia al extintor desde el lobby cumpla con la NTP 350.043-1", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 13, "Se debe recepcionar por correo el plano con la última versión", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paquetes de guantes desechables (02)", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paquetes de apósitos o gasas absorbentes de 32 pulgadas cuadradas (02)", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Rollo de esparadrapo 5cm x 4.5cm (01)", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Rollos de venda elástica de 2 pulgadas x 5 yardas (02)", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Rollos de venda elástica de 5 pulgadas x 5 yardas (02)", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Rollos de venda elástica de 8 pulgadas x 5 yardas (02)", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Venda triangular 40 x 40 x 56 pulgadas (01)", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paleta baja lengua (10)", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Venditas autoadhesivas (01)", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frasco de solución de cloruro de sodio al 9/1000 x litro (01)", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Lava ojo portátil (01)", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paquetes de gasa tipo jelonet para quemaduras (06)", 12, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Tijera de trauma punta roma (01)", 13, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Camilla rígida con protector de cabeza - inmovilizador de cabeza (01)", 14, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Camilla tipo canastilla (01)", 15, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frazada (01)", 16, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Resucitador manual o pocket mask (01)", 17, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Collarín regulable (01)", 18, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Torniquete (01)", 19, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Instructivo de primeros auxilios (01)", 20, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Registro de control de entrada y salida de insumos (01)", 21, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Fédula inmovilizadora (01)", 22, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frasco de Yodopovidona de 120 ml (01)", 23, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frasco de Agua Oxigenada de 120 ml (01)", 24, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frasco de Alcohol de 250 ml (01)", 25, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paquete de gasas estériles de 10x10 cm (05)", 26, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Paquete de Algodón por 100 gr (01)", 27, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Frasco de Colirio - lavado de ojos (02)", 28, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Pinza (01)", 29, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Cuerda para rescate 50 metros (01)", 30, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Extintor PQS 9 Kg mínimo (01)", 31, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 14, "Kit Antiderrame (01)", 32, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 15, "Médico ocupacional debe tener los archivos de descarga en una carpeta cifrada con contraseña", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 15, "Enfermero o médico deben tener toda la información física bajo llave", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 15, "Enfermero o médico ocupacional deben tener todos los archivos de su laptop o PC con contraseñas", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 15, "Solo médico o enfermero deben tener acceso a cualquier base de datos y aplicaciones", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 15, "Al enviar los EMOs a los trabajadores deben tener contraseña (DNI)", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE AIRE: Material particulado PM10 Bajo Volumen", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE AIRE: Material particulado PM 2.5 Bajo Volumen", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE AIRE: Monóxido de carbono (CO) 8 hora", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE AIRE: Dióxido de Nitrógeno (NO2) 1 hora", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE AIRE: Dióxido de azufre (SO2) 24 horas", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "Meteorología: Velocidad del viento, Dirección, Humedad relativa, Temperatura y Presión atmosférica", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 16, "MONITOREO DE CALIDAD DE RUIDO: Diurno", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Iluminación", 1, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Ruido por dosimetría", 2, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Polvo Inhalable", 3, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Polvo Respirable", 4, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Bacterias", 5, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Mohos y levaduras", 6, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Coliformes totales", 7, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "E. Coli", 8, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Recuento de Coliformes Totales (superficie irregular)", 9, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Postura", 10, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 17, "Psicosocial", 11, false, true, now });
            migrationBuilder.InsertData("ss_checklist_plantilla_item", ic, new object[] { 18, "Solicitar el usuario de la página de SINGERSOL a la Jefatura de Gestión Administrativa según el proyecto - Razón Social a declarar", 1, false, true, now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ssoma_accidente_trabajador_ssoma_accidente_incidente_accide",
                table: "ssoma_accidente_trabajador");

            migrationBuilder.DropTable(
                name: "ss_checklist_proyecto_item");

            migrationBuilder.DropTable(
                name: "ssoma_escuelita");

            migrationBuilder.DropTable(
                name: "ssoma_inhabilitacion");

            migrationBuilder.DropTable(
                name: "ss_checklist_plantilla_item");

            migrationBuilder.DropTable(
                name: "ss_checklist_proyecto");

            migrationBuilder.DropTable(
                name: "ss_checklist_plantilla");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ssoma_accidente_trabajador",
                table: "ssoma_accidente_trabajador");

            migrationBuilder.RenameTable(
                name: "ssoma_accidente_trabajador",
                newName: "ss_accidente_trabajador");

            migrationBuilder.RenameIndex(
                name: "ix_ssoma_accidente_trabajador_accidente_incidente_id",
                table: "ss_accidente_trabajador",
                newName: "ix_ss_accidente_trabajador_accidente_incidente_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ss_accidente_trabajador",
                table: "ss_accidente_trabajador",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_accidente_trabajador_ss_accidente_incidente_accidente_in",
                table: "ss_accidente_trabajador",
                column: "accidente_incidente_id",
                principalTable: "ss_accidente_incidente",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
