using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abril_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEvContratista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ev_contratista_plantilla (
    id            SERIAL PRIMARY KEY,
    area_nombre   VARCHAR(100) NOT NULL,
    puesto_evaluador VARCHAR(100) NOT NULL,
    criterio      TEXT NOT NULL,
    orden         INT  NOT NULL DEFAULT 0,
    activo        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_contratista (
    id                 SERIAL PRIMARY KEY,
    periodo_id         INT NOT NULL REFERENCES ev_periodo(id),
    proyecto_id        INT NOT NULL REFERENCES ""Projects""(""Id""),
    contributor_id     INT NOT NULL REFERENCES ""Contributors""(""Id""),
    evaluador_user_id  INT NOT NULL REFERENCES app_user(id),
    area_nombre        VARCHAR(100) NOT NULL,
    nota               NUMERIC(5,2),
    comentario         TEXT,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ,
    UNIQUE (periodo_id, proyecto_id, contributor_id, area_nombre, evaluador_user_id)
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_contratista_detalle (
    id                       SERIAL PRIMARY KEY,
    evaluacion_contratista_id INT NOT NULL REFERENCES ev_evaluacion_contratista(id) ON DELETE CASCADE,
    plantilla_id             INT REFERENCES ev_contratista_plantilla(id),
    criterio                 TEXT NOT NULL,
    puntaje                  INT NOT NULL CHECK (puntaje BETWEEN 0 AND 4),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS ev_evaluacion_contratista_detalle;
DROP TABLE IF EXISTS ev_evaluacion_contratista;
DROP TABLE IF EXISTS ev_contratista_plantilla;
");
        }
    }
}
