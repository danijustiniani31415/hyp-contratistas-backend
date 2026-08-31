using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAdministradorContributor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- contributor.email_administrador
                ALTER TABLE contributor
                    ADD COLUMN IF NOT EXISTS email_administrador text;

                -- worker_emos.fecha_lectura
                ALTER TABLE worker_emos
                    ADD COLUMN IF NOT EXISTS fecha_lectura date;

                -- ga_rendicion
                CREATE TABLE IF NOT EXISTS ga_rendicion (
                    id              serial PRIMARY KEY,
                    pdf_url         text NOT NULL,
                    pdf_item_id     text,
                    pdf_filename    text NOT NULL,
                    rendido_por_id  int  NOT NULL,
                    rendido_at      timestamptz NOT NULL
                );

                -- ga_solicitud_captura
                CREATE TABLE IF NOT EXISTS ga_solicitud_captura (
                    id              serial PRIMARY KEY,
                    trayecto_id     int  NOT NULL,
                    image_url       text NOT NULL,
                    image_item_id   text,
                    filename        text NOT NULL,
                    monto           numeric NOT NULL,
                    uploaded_by_id  int  NOT NULL,
                    uploaded_at     timestamptz NOT NULL
                );

                -- ga_solicitud_trayecto
                CREATE TABLE IF NOT EXISTS ga_solicitud_trayecto (
                    id                  serial PRIMARY KEY,
                    solicitud_id        int  NOT NULL,
                    orden               int  NOT NULL,
                    hora_salida         time NOT NULL,
                    hora_retorno        time,
                    motivo_id           int,
                    motivo_libre        text,
                    lugar_origen_id     int,
                    lugar_origen_libre  text,
                    lugar_destino_id    int,
                    lugar_destino_libre text
                );

                -- ga_trayecto
                CREATE TABLE IF NOT EXISTS ga_trayecto (
                    id               serial PRIMARY KEY,
                    lugar_origen_id  int     NOT NULL,
                    lugar_destino_id int     NOT NULL,
                    monto            numeric NOT NULL,
                    activo           boolean NOT NULL,
                    created_at       timestamptz NOT NULL
                );

                -- ss_contratista_rol
                CREATE TABLE IF NOT EXISTS ss_contratista_rol (
                    id     serial PRIMARY KEY,
                    nombre text NOT NULL UNIQUE
                );

                -- ss_contratista_usuario
                CREATE TABLE IF NOT EXISTS ss_contratista_usuario (
                    id              serial PRIMARY KEY,
                    contractor_id   int  NOT NULL,
                    user_id         int  NOT NULL,
                    rol_id          int  NOT NULL REFERENCES ss_contratista_rol(id),
                    scope           text NOT NULL DEFAULT 'TODOS',
                    activo          boolean NOT NULL DEFAULT true,
                    creado_en       timestamptz NOT NULL DEFAULT NOW(),
                    system_role_id  int,
                    creado_por      int
                );

                -- ss_contratista_usuario_proyecto
                CREATE TABLE IF NOT EXISTS ss_contratista_usuario_proyecto (
                    id                      serial PRIMARY KEY,
                    contratista_usuario_id  int NOT NULL REFERENCES ss_contratista_usuario(id) ON DELETE CASCADE,
                    proyecto_id             int NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración idempotente — Down vacío intencional
        }
    }
}
