using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPresupuestoMateriales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ss_consumo_carga",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    nombre_archivo = table.Column<string>(type: "text", nullable: false),
                    hash_archivo = table.Column<string>(type: "text", nullable: false),
                    fecha_min = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_max = table.Column<DateOnly>(type: "date", nullable: false),
                    total_lineas = table.Column<int>(type: "integer", nullable: false),
                    lineas_estandarizadas = table.Column<int>(type: "integer", nullable: false),
                    lineas_pendientes = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    subido_por = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_consumo_carga", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_consumo_carga_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_material_hito",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_material_hito", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_material_tipo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_material_tipo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ss_presupuesto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    total_calculado = table.Column<decimal>(type: "numeric", nullable: true),
                    creado_por = table.Column<int>(type: "integer", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_presupuesto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_material_familia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    nombre_normalizado = table.Column<string>(type: "text", nullable: false),
                    tipo_id = table.Column<int>(type: "integer", nullable: false),
                    variable_base = table.Column<string>(type: "text", nullable: false),
                    pertenece_ssoma = table.Column<bool>(type: "boolean", nullable: false),
                    unidad_medida = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_material_familia", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_material_familia_ss_material_tipo_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "ss_material_tipo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_presupuesto_personal_hito",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    presupuesto_id = table.Column<int>(type: "integer", nullable: false),
                    hito_id = table.Column<int>(type: "integer", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    semanas = table.Column<decimal>(type: "numeric", nullable: false),
                    costo_mensual = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_presupuesto_personal_hito", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_personal_hito_ss_material_hito_hito_id",
                        column: x => x.hito_id,
                        principalTable: "ss_material_hito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_personal_hito_ss_presupuesto_presupuesto_id",
                        column: x => x.presupuesto_id,
                        principalTable: "ss_presupuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_material_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    nombre_normalizado = table.Column<string>(type: "text", nullable: false),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    talla = table.Column<string>(type: "text", nullable: true),
                    dimension_norm = table.Column<string>(type: "text", nullable: true),
                    no_usar = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_material_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_material_item_ss_material_familia_familia_id",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_presupuesto_detalle",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    presupuesto_id = table.Column<int>(type: "integer", nullable: false),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    hito_id = table.Column<int>(type: "integer", nullable: true),
                    variable_base = table.Column<string>(type: "text", nullable: false),
                    ratio_aplicado = table.Column<decimal>(type: "numeric", nullable: true),
                    valor_driver = table.Column<decimal>(type: "numeric", nullable: true),
                    cantidad_calculada = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    es_manual = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_presupuesto_detalle", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_detalle_ss_material_familia_familia_id",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_detalle_ss_material_hito_hito_id",
                        column: x => x.hito_id,
                        principalTable: "ss_material_hito",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_detalle_ss_presupuesto_presupuesto_id",
                        column: x => x.presupuesto_id,
                        principalTable: "ss_presupuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_presupuesto_item_metrado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    presupuesto_id = table.Column<int>(type: "integer", nullable: false),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    metrado = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_presupuesto_item_metrado", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_item_metrado_ss_material_familia_familia_id",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_item_metrado_ss_presupuesto_presupuesto_id",
                        column: x => x.presupuesto_id,
                        principalTable: "ss_presupuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_presupuesto_seleccion_ratio",
                columns: table => new
                {
                    presupuesto_id = table.Column<int>(type: "integer", nullable: false),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    incluido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_presupuesto_seleccion_ratio", x => new { x.presupuesto_id, x.familia_id, x.project_id });
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_seleccion_ratio_ss_material_familia_familia_",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_presupuesto_seleccion_ratio_ss_presupuesto_presupuesto_id",
                        column: x => x.presupuesto_id,
                        principalTable: "ss_presupuesto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_ratio_proyecto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    familia_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    variable_base = table.Column<string>(type: "text", nullable: false),
                    cantidad_total = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_unitario_promedio = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_driver = table.Column<decimal>(type: "numeric", nullable: false),
                    ratio_cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    es_outlier = table.Column<bool>(type: "boolean", nullable: false),
                    calculado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_ratio_proyecto", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_ratio_proyecto_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_ratio_proyecto_ss_material_familia_familia_id",
                        column: x => x.familia_id,
                        principalTable: "ss_material_familia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ss_consumo_linea",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    carga_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    recurso_crudo = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    hito_id = table.Column<int>(type: "integer", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric", nullable: false),
                    precio_total = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_guia = table.Column<DateOnly>(type: "date", nullable: false),
                    estandarizado = table.Column<bool>(type: "boolean", nullable: false),
                    metodo_match = table.Column<string>(type: "text", nullable: true),
                    score_match = table.Column<decimal>(type: "numeric", nullable: true),
                    pertenece_ssoma = table.Column<bool>(type: "boolean", nullable: false),
                    estado_revision = table.Column<string>(type: "text", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_consumo_linea", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_consumo_linea_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_consumo_linea_ss_consumo_carga_carga_id",
                        column: x => x.carga_id,
                        principalTable: "ss_consumo_carga",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ss_consumo_linea_ss_material_hito_hito_id",
                        column: x => x.hito_id,
                        principalTable: "ss_material_hito",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ss_consumo_linea_ss_material_item_item_id",
                        column: x => x.item_id,
                        principalTable: "ss_material_item",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ss_material_alias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    texto_crudo = table.Column<string>(type: "text", nullable: false),
                    texto_crudo_norm = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    origen = table.Column<string>(type: "text", nullable: false),
                    confianza = table.Column<decimal>(type: "numeric", nullable: true),
                    confirmado_por = table.Column<int>(type: "integer", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_material_alias", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_material_alias_ss_material_item_item_id",
                        column: x => x.item_id,
                        principalTable: "ss_material_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_consumo_carga_project_id",
                table: "ss_consumo_carga",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_consumo_linea_carga_id",
                table: "ss_consumo_linea",
                column: "carga_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_consumo_linea_hito_id",
                table: "ss_consumo_linea",
                column: "hito_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_consumo_linea_item_id",
                table: "ss_consumo_linea",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_consumo_linea_project_id",
                table: "ss_consumo_linea",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_material_alias_item_id",
                table: "ss_material_alias",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_material_alias_texto_crudo_norm",
                table: "ss_material_alias",
                column: "texto_crudo_norm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ss_material_familia_tipo_id",
                table: "ss_material_familia",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_material_item_familia_id",
                table: "ss_material_item",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_project_id_version",
                table: "ss_presupuesto",
                columns: new[] { "project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_detalle_familia_id",
                table: "ss_presupuesto_detalle",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_detalle_hito_id",
                table: "ss_presupuesto_detalle",
                column: "hito_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_detalle_presupuesto_id",
                table: "ss_presupuesto_detalle",
                column: "presupuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_item_metrado_familia_id",
                table: "ss_presupuesto_item_metrado",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_item_metrado_presupuesto_id",
                table: "ss_presupuesto_item_metrado",
                column: "presupuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_personal_hito_hito_id",
                table: "ss_presupuesto_personal_hito",
                column: "hito_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_personal_hito_presupuesto_id",
                table: "ss_presupuesto_personal_hito",
                column: "presupuesto_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_presupuesto_seleccion_ratio_familia_id",
                table: "ss_presupuesto_seleccion_ratio",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_ratio_proyecto_familia_id_project_id",
                table: "ss_ratio_proyecto",
                columns: new[] { "familia_id", "project_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ss_ratio_proyecto_project_id",
                table: "ss_ratio_proyecto",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ss_consumo_linea");

            migrationBuilder.DropTable(
                name: "ss_material_alias");

            migrationBuilder.DropTable(
                name: "ss_presupuesto_detalle");

            migrationBuilder.DropTable(
                name: "ss_presupuesto_item_metrado");

            migrationBuilder.DropTable(
                name: "ss_presupuesto_personal_hito");

            migrationBuilder.DropTable(
                name: "ss_presupuesto_seleccion_ratio");

            migrationBuilder.DropTable(
                name: "ss_ratio_proyecto");

            migrationBuilder.DropTable(
                name: "ss_consumo_carga");

            migrationBuilder.DropTable(
                name: "ss_material_item");

            migrationBuilder.DropTable(
                name: "ss_material_hito");

            migrationBuilder.DropTable(
                name: "ss_presupuesto");

            migrationBuilder.DropTable(
                name: "ss_material_familia");

            migrationBuilder.DropTable(
                name: "ss_material_tipo");
        }
    }
}
