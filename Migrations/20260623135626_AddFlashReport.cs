using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE ss_charla DROP COLUMN IF EXISTS aprobado_en;");
            migrationBuilder.Sql("ALTER TABLE ss_charla DROP COLUMN IF EXISTS motivo_rechazo;");
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_charla' AND column_name='aprobado_por_id')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_charla' AND column_name='proyecto_id') THEN
                        ALTER TABLE ss_charla RENAME COLUMN aprobado_por_id TO proyecto_id;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='vecino_compromiso' AND column_name='observaciones') THEN
                        ALTER TABLE vecino_compromiso ADD COLUMN observaciones text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_charla' AND column_name='es_capacitacion_individual') THEN
                        ALTER TABLE ss_charla ADD COLUMN es_capacitacion_individual boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS vecino_compromiso_normativa (
                    vecino_compromiso_normativa_id serial PRIMARY KEY,
                    vecino_compromiso_id integer NOT NULL REFERENCES vecino_compromiso(vecino_compromiso_id) ON DELETE CASCADE,
                    archivo_url text NOT NULL,
                    original_file_name text,
                    created_date_time timestamp with time zone NOT NULL,
                    created_user_id integer NOT NULL,
                    updated_date_time timestamp with time zone,
                    updated_user_id integer,
                    active boolean NOT NULL,
                    state boolean NOT NULL
                );
                CREATE TABLE IF NOT EXISTS vecino_limpieza_tipo (
                    vecino_limpieza_tipo_id serial PRIMARY KEY,
                    descripcion text NOT NULL,
                    active boolean NOT NULL,
                    state boolean NOT NULL
                );
                CREATE TABLE IF NOT EXISTS vecino_limpieza (
                    vecino_limpieza_id serial PRIMARY KEY,
                    project_id integer NOT NULL,
                    vecino_limpieza_tipo_id integer NOT NULL REFERENCES vecino_limpieza_tipo(vecino_limpieza_tipo_id) ON DELETE CASCADE,
                    vecino_id integer REFERENCES vecino(vecino_id),
                    fecha date NOT NULL,
                    descripcion text,
                    atencion_archivo_url text,
                    atencion_original_file_name text,
                    atencion_vecino_compromiso_id integer,
                    created_date_time timestamp with time zone NOT NULL,
                    created_user_id integer NOT NULL,
                    updated_date_time timestamp with time zone,
                    updated_user_id integer,
                    active boolean NOT NULL,
                    state boolean NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_vecino_compromiso_normativa_vecino_compromiso_id ON vecino_compromiso_normativa(vecino_compromiso_id);
                CREATE INDEX IF NOT EXISTS ix_vecino_limpieza_vecino_id ON vecino_limpieza(vecino_id);
                CREATE INDEX IF NOT EXISTS ix_vecino_limpieza_vecino_limpieza_tipo_id ON vecino_limpieza(vecino_limpieza_tipo_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vecino_compromiso_normativa");

            migrationBuilder.DropTable(
                name: "vecino_limpieza");

            migrationBuilder.DropTable(
                name: "vecino_limpieza_tipo");

            migrationBuilder.DropColumn(
                name: "observaciones",
                table: "vecino_compromiso");

            migrationBuilder.DropColumn(
                name: "es_capacitacion_individual",
                table: "ss_charla");

            migrationBuilder.RenameColumn(
                name: "proyecto_id",
                table: "ss_charla",
                newName: "aprobado_por_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "aprobado_en",
                table: "ss_charla",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "ss_charla",
                type: "text",
                nullable: true);
        }
    }
}
