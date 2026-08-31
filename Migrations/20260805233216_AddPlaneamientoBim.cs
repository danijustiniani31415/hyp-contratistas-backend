using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaneamientoBim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── project: responsables y meta de Planeamiento BIM ────────────────
            migrationBuilder.AddColumn<decimal>(
                name: "meta_ppc",
                table: "project",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsable_planeamiento_bim",
                table: "project",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "responsable_planeamiento_bim_id",
                table: "project",
                type: "integer",
                nullable: true);

            // ── Catálogos globales ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "bim_macro_actividad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_macro_actividad", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bim_causa_no_cumplimiento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_causa_no_cumplimiento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bim_fase",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_fase", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bim_actividad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    macro_actividad_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_actividad", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_actividad_bim_macro_actividad_macro_actividad_id",
                        column: x => x.macro_actividad_id,
                        principalTable: "bim_macro_actividad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── Por proyecto ─────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "bim_proyecto_zona",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_proyecto_zona", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_proyecto_zona_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_zona_nivel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    zona_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_zona_nivel", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_zona_nivel_bim_proyecto_zona_zona_id",
                        column: x => x.zona_id,
                        principalTable: "bim_proyecto_zona",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_zona_sector",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    zona_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_zona_sector", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_zona_sector_bim_proyecto_zona_zona_id",
                        column: x => x.zona_id,
                        principalTable: "bim_proyecto_zona",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_proyecto_fase",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    fase_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin_meta = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin_real = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_proyecto_fase", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_proyecto_fase_bim_fase_fase_id",
                        column: x => x.fase_id,
                        principalTable: "bim_fase",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_proyecto_fase_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_registro_diario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    zona_id = table.Column<int>(type: "integer", nullable: false),
                    nivel_id = table.Column<int>(type: "integer", nullable: false),
                    sector_id = table.Column<int>(type: "integer", nullable: false),
                    actividad_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    cumplida = table.Column<bool>(type: "boolean", nullable: false),
                    causa_id = table.Column<int>(type: "integer", nullable: true),
                    causa_detalle = table.Column<string>(type: "text", nullable: true),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_user_id = table.Column<int>(type: "integer", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_registro_diario", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_bim_actividad_actividad_id",
                        column: x => x.actividad_id,
                        principalTable: "bim_actividad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_bim_causa_no_cumplimiento_causa_id",
                        column: x => x.causa_id,
                        principalTable: "bim_causa_no_cumplimiento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_bim_proyecto_zona_zona_id",
                        column: x => x.zona_id,
                        principalTable: "bim_proyecto_zona",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_bim_zona_nivel_nivel_id",
                        column: x => x.nivel_id,
                        principalTable: "bim_zona_nivel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_bim_zona_sector_sector_id",
                        column: x => x.sector_id,
                        principalTable: "bim_zona_sector",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bim_registro_diario_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_evidencia_foto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    created_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_evidencia_foto", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_evidencia_foto_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bim_bloqueo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_actualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bim_bloqueo", x => x.id);
                    table.ForeignKey(
                        name: "fk_bim_bloqueo_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Índices ──────────────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "ix_bim_actividad_macro_actividad_id_tipo_nombre",
                table: "bim_actividad",
                columns: new[] { "macro_actividad_id", "tipo", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bim_bloqueo_project_id",
                table: "bim_bloqueo",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_causa_no_cumplimiento_nombre",
                table: "bim_causa_no_cumplimiento",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bim_evidencia_foto_project_id_fecha",
                table: "bim_evidencia_foto",
                columns: new[] { "project_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_bim_fase_nombre",
                table: "bim_fase",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bim_macro_actividad_nombre",
                table: "bim_macro_actividad",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bim_proyecto_fase_fase_id",
                table: "bim_proyecto_fase",
                column: "fase_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_proyecto_fase_project_id",
                table: "bim_proyecto_fase",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_proyecto_zona_project_id",
                table: "bim_proyecto_zona",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_actividad_id",
                table: "bim_registro_diario",
                column: "actividad_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_causa_id",
                table: "bim_registro_diario",
                column: "causa_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_nivel_id",
                table: "bim_registro_diario",
                column: "nivel_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_sector_id",
                table: "bim_registro_diario",
                column: "sector_id");

            // UNIQUE: evita duplicados y define que una corrección dentro de la ventana de
            // edición sea UPDATE sobre esta fila, no un INSERT nuevo.
            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_unico",
                table: "bim_registro_diario",
                columns: new[] { "project_id", "zona_id", "nivel_id", "sector_id", "actividad_id", "fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bim_registro_diario_zona_id",
                table: "bim_registro_diario",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_zona_nivel_zona_id",
                table: "bim_zona_nivel",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "ix_bim_zona_sector_zona_id",
                table: "bim_zona_sector",
                column: "zona_id");

            // ── Catálogos semilla (idempotentes: ON CONFLICT DO NOTHING) ───────────
            migrationBuilder.Sql("""
                INSERT INTO bim_macro_actividad (nombre, orden) VALUES
                    ('Estructura Sótanos', 1),
                    ('Estructura Torre', 2),
                    ('Losa contraterreno', 3)
                ON CONFLICT (nombre) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO bim_fase (nombre, orden) VALUES
                    ('Diseño', 1),
                    ('Movimiento de Tierras', 2),
                    ('Casco', 3),
                    ('Acabados', 4),
                    ('Entrega', 5)
                ON CONFLICT (nombre) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO bim_causa_no_cumplimiento (nombre, orden) VALUES
                    ('Falta de material', 1),
                    ('Clima', 2),
                    ('Mano de obra', 3),
                    ('Diseño / Información pendiente', 4),
                    ('Otro', 5)
                ON CONFLICT (nombre) DO NOTHING;
                """);

            // Estructura Sótanos y Estructura Torre: actividades VERTICAL (por nivel,
            // columnas/placas primero) seguidas de las HORIZONTAL (vigas/losa).
            migrationBuilder.Sql("""
                INSERT INTO bim_actividad (macro_actividad_id, nombre, tipo, orden)
                SELECT bim_macro_actividad.id, v.nombre, v.tipo, v.orden
                FROM bim_macro_actividad, (VALUES
                    ('Acero de columnas y placas', 'VERTICAL', 1),
                    ('Instalaciones (IIEE, IISS, IIGG)', 'VERTICAL', 2),
                    ('Encofrado Columnas y Placas', 'VERTICAL', 3),
                    ('Vaciado de concreto', 'VERTICAL', 4),
                    ('Desencofrado / Resane / Curado', 'VERTICAL', 5),
                    ('Colocación acero en vigas', 'HORIZONTAL', 6),
                    ('Encofrado fondo de vigas', 'HORIZONTAL', 7),
                    ('Encofrado laterales de vigas', 'HORIZONTAL', 8),
                    ('Encofrado de losas', 'HORIZONTAL', 9),
                    ('Colocación de prelosas', 'HORIZONTAL', 10),
                    ('Instalaciones (IIEE, IISS, IIGG)', 'HORIZONTAL', 11),
                    ('Colocación de acero en losas', 'HORIZONTAL', 12),
                    ('Vaciado de concreto', 'HORIZONTAL', 13),
                    ('Desencofrado', 'HORIZONTAL', 14)
                ) AS v(nombre, tipo, orden)
                WHERE bim_macro_actividad.nombre IN ('Estructura Sótanos', 'Estructura Torre')
                ON CONFLICT (macro_actividad_id, tipo, nombre) DO NOTHING;
                """);

            // Losa contraterreno: es una losa única contra el terreno, sin elementos
            // verticales — solo recibe las actividades HORIZONTAL.
            migrationBuilder.Sql("""
                INSERT INTO bim_actividad (macro_actividad_id, nombre, tipo, orden)
                SELECT bim_macro_actividad.id, v.nombre, v.tipo, v.orden
                FROM bim_macro_actividad, (VALUES
                    ('Colocación acero en vigas', 'HORIZONTAL', 1),
                    ('Encofrado fondo de vigas', 'HORIZONTAL', 2),
                    ('Encofrado laterales de vigas', 'HORIZONTAL', 3),
                    ('Encofrado de losas', 'HORIZONTAL', 4),
                    ('Colocación de prelosas', 'HORIZONTAL', 5),
                    ('Instalaciones (IIEE, IISS, IIGG)', 'HORIZONTAL', 6),
                    ('Colocación de acero en losas', 'HORIZONTAL', 7),
                    ('Vaciado de concreto', 'HORIZONTAL', 8),
                    ('Desencofrado', 'HORIZONTAL', 9)
                ) AS v(nombre, tipo, orden)
                WHERE bim_macro_actividad.nombre = 'Losa contraterreno'
                ON CONFLICT (macro_actividad_id, tipo, nombre) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "bim_registro_diario");
            migrationBuilder.DropTable(name: "bim_evidencia_foto");
            migrationBuilder.DropTable(name: "bim_bloqueo");
            migrationBuilder.DropTable(name: "bim_proyecto_fase");
            migrationBuilder.DropTable(name: "bim_zona_nivel");
            migrationBuilder.DropTable(name: "bim_zona_sector");
            migrationBuilder.DropTable(name: "bim_actividad");
            migrationBuilder.DropTable(name: "bim_proyecto_zona");
            migrationBuilder.DropTable(name: "bim_causa_no_cumplimiento");
            migrationBuilder.DropTable(name: "bim_fase");
            migrationBuilder.DropTable(name: "bim_macro_actividad");

            migrationBuilder.DropColumn(
                name: "meta_ppc",
                table: "project");

            migrationBuilder.DropColumn(
                name: "responsable_planeamiento_bim",
                table: "project");

            migrationBuilder.DropColumn(
                name: "responsable_planeamiento_bim_id",
                table: "project");
        }
    }
}
