-- Ejecutar en desarrollo primero, luego en producción.
-- Tabla: charla diaria obligatoria del contratista, ligada al tareo de Control de Acceso.

CREATE TABLE ss_charla_contratista (
    id                   SERIAL PRIMARY KEY,
    proyecto_id          INTEGER NOT NULL REFERENCES project(project_id),
    empresa_id           INTEGER NOT NULL REFERENCES contributor(contributor_id),
    fecha                DATE NOT NULL,
    tema                 VARCHAR(200) NOT NULL,
    descripcion          TEXT NULL,
    evidencia_url        VARCHAR(1000) NULL,
    evidencia_nombre     VARCHAR(300) NULL,
    subido_por_user_id   INTEGER NULL REFERENCES app_user(user_id),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    state                BOOLEAN NOT NULL DEFAULT true
);

-- Evita que una misma empresa registre más de una charla activa por proyecto/día
CREATE UNIQUE INDEX ux_ss_charla_contratista_unica_dia
    ON ss_charla_contratista (proyecto_id, empresa_id, fecha)
    WHERE state = true;

CREATE INDEX ix_ss_charla_contratista_empresa_fecha
    ON ss_charla_contratista (empresa_id, fecha);
