using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSsEmpresaContratista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: drop old FKs only if they still exist
            migrationBuilder.Sql("ALTER TABLE ss_empresa_proyecto DROP CONSTRAINT IF EXISTS fk_ss_empresa_proyecto_ss_empresa_contratista_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_eval_supervisor DROP CONSTRAINT IF EXISTS fk_ss_eval_supervisor_ss_empresa_contratista_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_hab_bloqueo_log DROP CONSTRAINT IF EXISTS fk_ss_hab_bloqueo_log_ss_empresa_contratista_empresa_propietar;");
            migrationBuilder.Sql("ALTER TABLE ss_hab_bloqueo_log DROP CONSTRAINT IF EXISTS fk_ss_hab_bloqueo_log_ss_empresa_contratista_empresa_solicitan;");
            migrationBuilder.Sql("ALTER TABLE ss_hab_empresa DROP CONSTRAINT IF EXISTS fk_ss_hab_empresa_ss_empresa_contratista_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_induccion DROP CONSTRAINT IF EXISTS fk_ss_induccion_ss_empresa_contratista_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_reset_token DROP CONSTRAINT IF EXISTS fk_ss_reset_token_ss_empresa_contratista_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_sctr_vidaley DROP CONSTRAINT IF EXISTS fk_ss_sctr_vidaley_ss_empresa_contratista_empresa_id;");

            // Data migration: only runs if ss_empresa_contratista still exists
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ss_empresa_contratista') THEN
                    UPDATE ss_hab_empresa t
                      SET empresa_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_induccion t
                      SET empresa_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_sctr_vidaley t
                      SET empresa_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_empresa_proyecto t
                      SET empresa_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_hab_bloqueo_log t
                      SET empresa_solicitante_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_solicitante_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_hab_bloqueo_log t
                      SET empresa_propietaria_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_propietaria_id AND sec.id_legacy IS NOT NULL;

                    UPDATE ss_eval_supervisor t
                      SET empresa_id = sec.id_legacy
                      FROM ss_empresa_contratista sec
                      WHERE sec.id = t.empresa_id AND sec.id_legacy IS NOT NULL;
                  END IF;
                END $$;
                """);

            // Orphan check
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM ss_hab_empresa
                    WHERE empresa_id NOT IN (SELECT contributor_id FROM contributor)
                  ) THEN
                    RAISE EXCEPTION 'ss_hab_empresa contiene empresa_id sin correspondencia en contributor';
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM ss_induccion
                    WHERE empresa_id NOT IN (SELECT contributor_id FROM contributor)
                  ) THEN
                    RAISE EXCEPTION 'ss_induccion contiene empresa_id sin correspondencia en contributor';
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM ss_sctr_vidaley
                    WHERE empresa_id NOT IN (SELECT contributor_id FROM contributor)
                  ) THEN
                    RAISE EXCEPTION 'ss_sctr_vidaley contiene empresa_id sin correspondencia en contributor';
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM ss_empresa_proyecto
                    WHERE empresa_id NOT IN (SELECT contributor_id FROM contributor)
                  ) THEN
                    RAISE EXCEPTION 'ss_empresa_proyecto contiene empresa_id sin correspondencia en contributor';
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM ss_eval_supervisor
                    WHERE empresa_id NOT IN (SELECT contributor_id FROM contributor)
                  ) THEN
                    RAISE EXCEPTION 'ss_eval_supervisor contiene empresa_id sin correspondencia en contributor';
                  END IF;
                END $$;
                """);

            // Idempotent: drop table and related column/index only if they exist
            migrationBuilder.Sql("DROP TABLE IF EXISTS ss_empresa_contratista CASCADE;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ss_reset_token_empresa_id;");
            migrationBuilder.Sql("ALTER TABLE ss_reset_token DROP COLUMN IF EXISTS empresa_id;");

            // Idempotent: add new FKs only if they don't exist yet
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_empresa_proyecto_contributor_empresa_id') THEN
                    ALTER TABLE ss_empresa_proyecto ADD CONSTRAINT fk_ss_empresa_proyecto_contributor_empresa_id
                      FOREIGN KEY (empresa_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_eval_supervisor_contributor_empresa_id') THEN
                    ALTER TABLE ss_eval_supervisor ADD CONSTRAINT fk_ss_eval_supervisor_contributor_empresa_id
                      FOREIGN KEY (empresa_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_hab_bloqueo_log_contributor_empresa_propietaria_id') THEN
                    ALTER TABLE ss_hab_bloqueo_log ADD CONSTRAINT fk_ss_hab_bloqueo_log_contributor_empresa_propietaria_id
                      FOREIGN KEY (empresa_propietaria_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_hab_bloqueo_log_contributor_empresa_solicitante_id') THEN
                    ALTER TABLE ss_hab_bloqueo_log ADD CONSTRAINT fk_ss_hab_bloqueo_log_contributor_empresa_solicitante_id
                      FOREIGN KEY (empresa_solicitante_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_hab_empresa_contributor_empresa_id') THEN
                    ALTER TABLE ss_hab_empresa ADD CONSTRAINT fk_ss_hab_empresa_contributor_empresa_id
                      FOREIGN KEY (empresa_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_induccion_contributor_empresa_id') THEN
                    ALTER TABLE ss_induccion ADD CONSTRAINT fk_ss_induccion_contributor_empresa_id
                      FOREIGN KEY (empresa_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ss_sctr_vidaley_contributor_empresa_id') THEN
                    ALTER TABLE ss_sctr_vidaley ADD CONSTRAINT fk_ss_sctr_vidaley_contributor_empresa_id
                      FOREIGN KEY (empresa_id) REFERENCES contributor(contributor_id) ON DELETE CASCADE;
                  END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_empresa_proyecto_contributor_empresa_id",
                table: "ss_empresa_proyecto");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_eval_supervisor_contributor_empresa_id",
                table: "ss_eval_supervisor");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_hab_bloqueo_log_contributor_empresa_propietaria_id",
                table: "ss_hab_bloqueo_log");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_hab_bloqueo_log_contributor_empresa_solicitante_id",
                table: "ss_hab_bloqueo_log");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_hab_empresa_contributor_empresa_id",
                table: "ss_hab_empresa");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_induccion_contributor_empresa_id",
                table: "ss_induccion");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_sctr_vidaley_contributor_empresa_id",
                table: "ss_sctr_vidaley");

            migrationBuilder.AddColumn<int>(
                name: "empresa_id",
                table: "ss_reset_token",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ss_empresa_contratista",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proyecto_id = table.Column<int>(type: "integer", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    activo_retirado = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    direccion = table.Column<string>(type: "text", nullable: true),
                    email_admin = table.Column<string>(type: "text", nullable: true),
                    email_gerente = table.Column<string>(type: "text", nullable: true),
                    email_residente = table.Column<string>(type: "text", nullable: true),
                    email_ssoma = table.Column<string>(type: "text", nullable: true),
                    id_legacy = table.Column<int>(type: "integer", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    nombre_comercial = table.Column<string>(type: "text", nullable: true),
                    partida_registral = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    razon_social = table.Column<string>(type: "text", nullable: false),
                    rubro = table.Column<string>(type: "text", nullable: true),
                    ruc = table.Column<string>(type: "text", nullable: true),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ss_empresa_contratista", x => x.id);
                    table.ForeignKey(
                        name: "fk_ss_empresa_contratista_project_proyecto_id",
                        column: x => x.proyecto_id,
                        principalTable: "project",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ss_reset_token_empresa_id",
                table: "ss_reset_token",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ss_empresa_contratista_proyecto_id",
                table: "ss_empresa_contratista",
                column: "proyecto_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_empresa_proyecto_ss_empresa_contratista_empresa_id",
                table: "ss_empresa_proyecto",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_eval_supervisor_ss_empresa_contratista_empresa_id",
                table: "ss_eval_supervisor",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_hab_bloqueo_log_ss_empresa_contratista_empresa_propietar",
                table: "ss_hab_bloqueo_log",
                column: "empresa_propietaria_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_hab_bloqueo_log_ss_empresa_contratista_empresa_solicitan",
                table: "ss_hab_bloqueo_log",
                column: "empresa_solicitante_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_hab_empresa_ss_empresa_contratista_empresa_id",
                table: "ss_hab_empresa",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_induccion_ss_empresa_contratista_empresa_id",
                table: "ss_induccion",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ss_reset_token_ss_empresa_contratista_empresa_id",
                table: "ss_reset_token",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ss_sctr_vidaley_ss_empresa_contratista_empresa_id",
                table: "ss_sctr_vidaley",
                column: "empresa_id",
                principalTable: "ss_empresa_contratista",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
