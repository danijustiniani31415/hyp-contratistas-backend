using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDriverColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- ss_presupuesto_detalle: drops y renames idempotentes
                ALTER TABLE ss_presupuesto_detalle DROP CONSTRAINT IF EXISTS fk_ss_presupuesto_detalle_ss_material_hito_hito_id;
                DROP INDEX IF EXISTS ix_ss_presupuesto_detalle_hito_id;
                ALTER TABLE ss_presupuesto_detalle DROP COLUMN IF EXISTS hito_id;
                ALTER TABLE ss_presupuesto DROP COLUMN IF EXISTS nombre;
                ALTER TABLE ss_presupuesto DROP COLUMN IF EXISTS total_calculado;

                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto_detalle' AND column_name='total') THEN
                        ALTER TABLE ss_presupuesto_detalle RENAME COLUMN total TO total_estimado;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto_detalle' AND column_name='ratio_aplicado') THEN
                        ALTER TABLE ss_presupuesto_detalle RENAME COLUMN ratio_aplicado TO precio_manual;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto_detalle' AND column_name='es_manual') THEN
                        ALTER TABLE ss_presupuesto_detalle RENAME COLUMN es_manual TO tiene_historia;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto_detalle' AND column_name='cantidad_calculada') THEN
                        ALTER TABLE ss_presupuesto_detalle RENAME COLUMN cantidad_calculada TO ratio_recomendado;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto' AND column_name='creado_por') THEN
                        ALTER TABLE ss_presupuesto RENAME COLUMN creado_por TO generado_por;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto' AND column_name='creado_en') THEN
                        ALTER TABLE ss_presupuesto RENAME COLUMN creado_en TO generado_en;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_presupuesto' AND column_name='actualizado_en') THEN
                        ALTER TABLE ss_presupuesto RENAME COLUMN actualizado_en TO aprobado_en;
                    END IF;
                END $$;

                -- ss_presupuesto_detalle: alter y adds idempotentes
                UPDATE ss_presupuesto_detalle SET valor_driver = 0 WHERE valor_driver IS NULL;
                ALTER TABLE ss_presupuesto_detalle ALTER COLUMN valor_driver SET NOT NULL;
                ALTER TABLE ss_presupuesto_detalle ALTER COLUMN valor_driver SET DEFAULT 0;
                ALTER TABLE ss_presupuesto_detalle ADD COLUMN IF NOT EXISTS cantidad_estimada numeric NOT NULL DEFAULT 0;
                ALTER TABLE ss_presupuesto_detalle ADD COLUMN IF NOT EXISTS cantidad_manual numeric;
                ALTER TABLE ss_presupuesto_detalle ADD COLUMN IF NOT EXISTS n_proyectos_base integer NOT NULL DEFAULT 0;
                ALTER TABLE ss_presupuesto_detalle ADD COLUMN IF NOT EXISTS notas_linea text;
                ALTER TABLE ss_presupuesto_detalle ADD COLUMN IF NOT EXISTS tipo_id integer NOT NULL DEFAULT 0;

                -- ss_presupuesto: adds idempotentes
                ALTER TABLE ss_presupuesto ADD COLUMN IF NOT EXISTS area_usada numeric NOT NULL DEFAULT 0;
                ALTER TABLE ss_presupuesto ADD COLUMN IF NOT EXISTS hh_usado numeric NOT NULL DEFAULT 0;
                ALTER TABLE ss_presupuesto ADD COLUMN IF NOT EXISTS notas text;
                ALTER TABLE ss_presupuesto ADD COLUMN IF NOT EXISTS total_estimado numeric NOT NULL DEFAULT 0;
                ALTER TABLE ss_presupuesto ADD COLUMN IF NOT EXISTS trabajadores_usados integer NOT NULL DEFAULT 0;

                -- project: adds idempotentes
                ALTER TABLE project ADD COLUMN IF NOT EXISTS activo text;
                ALTER TABLE project ADD COLUMN IF NOT EXISTS hh_fuente text;

                -- ss_control_semana: create idempotente
                CREATE TABLE IF NOT EXISTS ss_control_semana (
                    id integer GENERATED BY DEFAULT AS IDENTITY,
                    presupuesto_id integer NOT NULL,
                    project_id integer NOT NULL,
                    semana_num integer NOT NULL,
                    fecha_inicio date NOT NULL,
                    fecha_fin date NOT NULL,
                    estado text NOT NULL,
                    observaciones text,
                    registrado_por integer,
                    registrado_en timestamp with time zone NOT NULL,
                    cerrado_en timestamp with time zone,
                    CONSTRAINT pk_ss_control_semana PRIMARY KEY (id),
                    CONSTRAINT fk_ss_control_semana_ss_presupuesto_presupuesto_id
                        FOREIGN KEY (presupuesto_id) REFERENCES ss_presupuesto (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_ss_control_semana_presupuesto_id ON ss_control_semana (presupuesto_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS ss_control_semana;
                ALTER TABLE project DROP COLUMN IF EXISTS activo;
                ALTER TABLE project DROP COLUMN IF EXISTS hh_fuente;
                """);
        }
    }
}
