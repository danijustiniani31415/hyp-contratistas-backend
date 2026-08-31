-- Migración manual (pgAdmin) — módulo Observaciones de Arquitectura Comercial
-- Ejecutar directamente contra la BD PostgreSQL. No usar dotnet ef.

CREATE TABLE IF NOT EXISTS ac_observaciones (
    id                    SERIAL PRIMARY KEY,
    proyecto_id           INTEGER NOT NULL REFERENCES project (project_id) ON DELETE CASCADE,
    codigo                TEXT NOT NULL,
    fecha                 TIMESTAMP NOT NULL,
    persona_reporta       TEXT,
    empresa_reporta       TEXT,
    lugar                 TEXT,
    descripcion           TEXT NOT NULL,
    plazo_levantamiento   TIMESTAMP,
    partida_reportada     TEXT,
    estado                TEXT NOT NULL DEFAULT 'Pendiente',
    tipo_observacion      TEXT,
    area_responsable      TEXT,
    ejecutor              TEXT,
    observacion           TEXT,
    levantamiento         TEXT,
    estado_cierre         TEXT,
    creado_por            TEXT,
    origen                TEXT NOT NULL DEFAULT 'Nuevo',
    created_at            TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    fecha_levantamiento   TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_ac_observaciones_proyecto_id ON ac_observaciones (proyecto_id);
CREATE INDEX IF NOT EXISTS ix_ac_observaciones_estado      ON ac_observaciones (estado);
CREATE INDEX IF NOT EXISTS ix_ac_observaciones_codigo       ON ac_observaciones (codigo);

CREATE TABLE IF NOT EXISTS ac_observacion_fotos (
    id               SERIAL PRIMARY KEY,
    observacion_id   INTEGER NOT NULL REFERENCES ac_observaciones (id) ON DELETE CASCADE,
    tipo             TEXT NOT NULL DEFAULT 'Observacion',
    url              TEXT NOT NULL,
    orden            INTEGER NOT NULL DEFAULT 0,
    created_at       TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);

CREATE INDEX IF NOT EXISTS ix_ac_observacion_fotos_observacion_id ON ac_observacion_fotos (observacion_id);
