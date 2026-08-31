using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashReportEntregablesRm050 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ss_entregable_tipo (
                    id SERIAL PRIMARY KEY,
                    nombre VARCHAR(200) NOT NULL,
                    orden INT NOT NULL DEFAULT 0,
                    activo BOOLEAN NOT NULL DEFAULT true
                );

                CREATE TABLE IF NOT EXISTS ss_entregable (
                    id SERIAL PRIMARY KEY,
                    accidente_incidente_id INT NOT NULL REFERENCES ss_accidente_incidente(id) ON DELETE CASCADE,
                    tipo_id INT NOT NULL REFERENCES ss_entregable_tipo(id),
                    estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
                    fecha_limite DATE,
                    url_archivo VARCHAR(500),
                    nombre_archivo VARCHAR(300),
                    observacion TEXT,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ
                );

                CREATE TABLE IF NOT EXISTS ss_entregable_responsable (
                    id SERIAL PRIMARY KEY,
                    entregable_id INT NOT NULL REFERENCES ss_entregable(id) ON DELETE CASCADE,
                    worker_id INT REFERENCES workers(id),
                    nombre VARCHAR(200) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ss_investigacion_rm050 (
                    id SERIAL PRIMARY KEY,
                    accidente_incidente_id INT NOT NULL UNIQUE REFERENCES ss_accidente_incidente(id) ON DELETE CASCADE,
                    descripcion_detallada TEXT,
                    mecanismo TEXT,
                    agente_causante TEXT,
                    actos_subestandar TEXT,
                    condiciones_subestandar TEXT,
                    factores_personales TEXT,
                    factores_trabajo TEXT,
                    dias_perdidos INT,
                    tipo_accidente VARCHAR(100),
                    arbol_causas_url VARCHAR(500),
                    elaborado_por_nombre VARCHAR(200),
                    elaborado_por_cargo VARCHAR(200),
                    elaborado_por_fecha DATE,
                    aprobado_por_nombre VARCHAR(200),
                    aprobado_por_cargo VARCHAR(200),
                    estado VARCHAR(50) NOT NULL DEFAULT 'Borrador',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ
                );

                CREATE TABLE IF NOT EXISTS ss_accion_correctiva (
                    id SERIAL PRIMARY KEY,
                    investigacion_id INT NOT NULL REFERENCES ss_investigacion_rm050(id) ON DELETE CASCADE,
                    descripcion TEXT NOT NULL,
                    tipo VARCHAR(100),
                    responsable_nombre VARCHAR(200),
                    responsable_worker_id INT REFERENCES workers(id),
                    fecha_compromiso DATE,
                    fecha_cumplimiento DATE,
                    estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
                    evidencia_url VARCHAR(500),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                INSERT INTO ss_entregable_tipo (nombre, orden) VALUES
                    ('Flash Report', 1),
                    ('ATS', 2),
                    ('PETAR', 3),
                    ('PETS', 4),
                    ('IPERC', 5),
                    ('Checklist de inspección', 6),
                    ('Manifestación del afectado', 7),
                    ('Manifestación del supervisor', 8),
                    ('Manifestación del testigo 1', 9),
                    ('Manifestación del testigo 2', 10),
                    ('Evidencia fotográfica', 11),
                    ('Informe de investigación RM-050', 12),
                    ('Registro de accidentes', 13),
                    ('Descanso médico', 14),
                    ('Alta médica', 15),
                    ('Evidencia de modificación de PETS', 16),
                    ('Evidencia de modificación de IPERC', 17),
                    ('Evidencia de acta de comité', 18),
                    ('Eficacia de los controles', 19),
                    ('Herramientas de gestión', 20);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS ss_accion_correctiva;
                DROP TABLE IF EXISTS ss_entregable_responsable;
                DROP TABLE IF EXISTS ss_investigacion_rm050;
                DROP TABLE IF EXISTS ss_entregable;
                DROP TABLE IF EXISTS ss_entregable_tipo;
                """);
        }
    }
}
