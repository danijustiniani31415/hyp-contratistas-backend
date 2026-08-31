using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashReportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    -- Renames on ss_accidente_incidente
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='usuario_id')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='worker_id') THEN
                        ALTER TABLE ss_accidente_incidente RENAME COLUMN usuario_id TO worker_id;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='tipo')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='lugar_exacto') THEN
                        ALTER TABLE ss_accidente_incidente RENAME COLUMN tipo TO lugar_exacto;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='responsable_id')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='partida_id') THEN
                        ALTER TABLE ss_accidente_incidente RENAME COLUMN responsable_id TO partida_id;
                    END IF;
                    -- vecino_compromiso
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='vecino_compromiso' AND column_name='fecha_fin_municipalidad') THEN
                        ALTER TABLE vecino_compromiso ADD COLUMN fecha_fin_municipalidad date;
                    END IF;
                    -- ss_accidente_incidente new columns
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='acciones_inmediatas') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN acciones_inmediatas text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='anios_experiencia') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN anios_experiencia integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='celular_trabajador') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN celular_trabajador text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='codigo') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN codigo text NOT NULL DEFAULT ''; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='consecuencia_potencial_personal') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN consecuencia_potencial_personal integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='consecuencia_real_personal') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN consecuencia_real_personal integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='contributor_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN contributor_id integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='dano_proceso') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN dano_proceso text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='edad') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN edad integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='elaborado_por_cargo') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN elaborado_por_cargo text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='elaborado_por_email') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN elaborado_por_email text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='elaborado_por_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN elaborado_por_id integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='elaborado_por_nombre') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN elaborado_por_nombre text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='elaborado_por_telefono') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN elaborado_por_telefono text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='empresa_abril_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN empresa_abril_id integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='enviado') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN enviado boolean NOT NULL DEFAULT false; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='etapa_proyecto_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN etapa_proyecto_id integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='fecha_envio') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN fecha_envio timestamp with time zone; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='hora') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN hora interval; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='jefe_inmediato_nombre') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN jefe_inmediato_nombre text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='parte_afectada_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN parte_afectada_id integer; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='puesto_trabajo') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN puesto_trabajo text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='tipo_id') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN tipo_id integer NOT NULL DEFAULT 0; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='trabajador_nombre') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN trabajador_nombre text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='url_foto1') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN url_foto1 text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='url_foto2') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN url_foto2 text; END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ss_accidente_incidente' AND column_name='url_pdf_sharepoint') THEN ALTER TABLE ss_accidente_incidente ADD COLUMN url_pdf_sharepoint text; END IF;
                END $$;

                CREATE TABLE IF NOT EXISTS ss_charla_archivo (
                    id serial PRIMARY KEY,
                    charla_id integer NOT NULL REFERENCES ss_charla(id) ON DELETE CASCADE,
                    url varchar(1000) NOT NULL,
                    nombre varchar(300) NOT NULL,
                    sp_id varchar(200),
                    state boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_empresa_abril (
                    id serial PRIMARY KEY,
                    razon_social text NOT NULL,
                    ruc text,
                    activa boolean NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_flash_descanso (
                    id serial PRIMARY KEY,
                    accidente_incidente_id integer NOT NULL REFERENCES ss_accidente_incidente(id) ON DELETE CASCADE,
                    fecha_inicio timestamp with time zone NOT NULL,
                    fecha_fin timestamp with time zone NOT NULL,
                    observacion text,
                    created_at timestamp with time zone NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_flash_etapa_proyecto (
                    id serial PRIMARY KEY,
                    nombre text NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_flash_parte_afectada (
                    id serial PRIMARY KEY,
                    nombre text NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_flash_partida (
                    id serial PRIMARY KEY,
                    nombre text NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ssoma_flash_tipo (
                    id serial PRIMARY KEY,
                    codigo text NOT NULL,
                    nombre text NOT NULL,
                    orden integer NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_ss_accidente_incidente_empresa_abril_id ON ss_accidente_incidente(empresa_abril_id);
                CREATE INDEX IF NOT EXISTS ix_ss_accidente_incidente_etapa_proyecto_id ON ss_accidente_incidente(etapa_proyecto_id);
                CREATE INDEX IF NOT EXISTS ix_ss_accidente_incidente_parte_afectada_id ON ss_accidente_incidente(parte_afectada_id);
                CREATE INDEX IF NOT EXISTS ix_ss_accidente_incidente_partida_id ON ss_accidente_incidente(partida_id);
                CREATE INDEX IF NOT EXISTS ix_ss_accidente_incidente_tipo_id ON ss_accidente_incidente(tipo_id);
                CREATE INDEX IF NOT EXISTS ix_ss_charla_archivo_charla_id ON ss_charla_archivo(charla_id);
                CREATE INDEX IF NOT EXISTS ix_ssoma_flash_descanso_accidente_incidente_id ON ssoma_flash_descanso(accidente_incidente_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_incidente_ssoma_empresa_abril_empresa_abril_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_incidente_ssoma_flash_etapa_proyecto_etapa_pro",
                table: "ss_accidente_incidente");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_incidente_ssoma_flash_parte_afectada_parte_afe",
                table: "ss_accidente_incidente");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_incidente_ssoma_flash_partida_partida_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropForeignKey(
                name: "fk_ss_accidente_incidente_ssoma_flash_tipo_tipo_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropTable(
                name: "ss_charla_archivo");

            migrationBuilder.DropTable(
                name: "ssoma_empresa_abril");

            migrationBuilder.DropTable(
                name: "ssoma_flash_descanso");

            migrationBuilder.DropTable(
                name: "ssoma_flash_etapa_proyecto");

            migrationBuilder.DropTable(
                name: "ssoma_flash_parte_afectada");

            migrationBuilder.DropTable(
                name: "ssoma_flash_partida");

            migrationBuilder.DropTable(
                name: "ssoma_flash_tipo");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_incidente_empresa_abril_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_incidente_etapa_proyecto_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_incidente_parte_afectada_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_incidente_partida_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropIndex(
                name: "ix_ss_accidente_incidente_tipo_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "fecha_fin_municipalidad",
                table: "vecino_compromiso");

            migrationBuilder.DropColumn(
                name: "acciones_inmediatas",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "anios_experiencia",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "celular_trabajador",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "consecuencia_potencial_personal",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "consecuencia_real_personal",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "contributor_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "dano_proceso",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "edad",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "elaborado_por_cargo",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "elaborado_por_email",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "elaborado_por_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "elaborado_por_nombre",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "elaborado_por_telefono",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "empresa_abril_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "enviado",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "etapa_proyecto_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "fecha_envio",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "hora",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "jefe_inmediato_nombre",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "parte_afectada_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "puesto_trabajo",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "tipo_id",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "trabajador_nombre",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "url_foto1",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "url_foto2",
                table: "ss_accidente_incidente");

            migrationBuilder.DropColumn(
                name: "url_pdf_sharepoint",
                table: "ss_accidente_incidente");

            migrationBuilder.RenameColumn(
                name: "worker_id",
                table: "ss_accidente_incidente",
                newName: "usuario_id");

            migrationBuilder.RenameColumn(
                name: "partida_id",
                table: "ss_accidente_incidente",
                newName: "responsable_id");

            migrationBuilder.RenameColumn(
                name: "lugar_exacto",
                table: "ss_accidente_incidente",
                newName: "tipo");
        }
    }
}
