-- ============================================================================
-- Módulo Tareos (Arquitectura Comercial)
-- ============================================================================
-- ac_tareo_enrolamiento: embedding facial de referencia por trabajador (face-api.js, 128 floats)
-- ac_tareo_registro:     una fila por cada marcación (inicio jornada / inicio almuerzo / retorno / fin jornada)
-- Idempotente: se puede correr más de una vez sin duplicar.
-- ============================================================================

BEGIN;

-- ── Geolocalización de proyectos (para geofencing) ──────────────────────────
ALTER TABLE project
    ADD COLUMN IF NOT EXISTS lat NUMERIC(10,7),
    ADD COLUMN IF NOT EXISTS lng NUMERIC(10,7),
    ADD COLUMN IF NOT EXISTS radio_geofence_metros NUMERIC(8,2) NOT NULL DEFAULT 300;

-- ── Enrolamiento facial ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ac_tareo_enrolamiento (
    id                 SERIAL PRIMARY KEY,
    worker_id          INTEGER NOT NULL UNIQUE REFERENCES workers (id),
    embedding          REAL[] NOT NULL,
    foto_url           TEXT NOT NULL,
    consentimiento_en  TIMESTAMPTZ NOT NULL,
    activo             BOOLEAN NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ
);

-- ── Registros de marcado ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ac_tareo_registro (
    id                  SERIAL PRIMARY KEY,
    worker_id           INTEGER NOT NULL REFERENCES workers (id),
    tipo                VARCHAR(20) NOT NULL,       -- INICIO_JORNADA | INICIO_ALMUERZO | RETORNO | FIN_JORNADA
    fecha               DATE NOT NULL,
    hora_servidor       TIMESTAMPTZ NOT NULL,
    hora_dispositivo    TIMESTAMPTZ,
    foto_url            TEXT NOT NULL,
    foto_hash           TEXT NOT NULL,
    idempotency_key     UUID NOT NULL,
    lat                 NUMERIC(10,7),
    lng                 NUMERIC(10,7),
    precision_metros    NUMERIC(8,2),
    project_id          INTEGER REFERENCES project (project_id),
    distancia_metros    NUMERIC(8,2),
    face_match_score    NUMERIC(5,4),
    estado              VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE',  -- VERIFICADO | REVISAR | RECHAZADO | SIN_ENROLAR
    motivo_revision     TEXT,
    revisado_por        INTEGER REFERENCES app_user (user_id),
    revisado_en         TIMESTAMPTZ,
    ip_origen           TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_tareo_worker_fecha       ON ac_tareo_registro (worker_id, fecha);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tareo_worker_fecha_tipo ON ac_tareo_registro (worker_id, fecha, tipo);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tareo_idempotency_key   ON ac_tareo_registro (idempotency_key);
CREATE INDEX IF NOT EXISTS ix_tareo_estado ON ac_tareo_registro (estado) WHERE estado = 'REVISAR';
CREATE INDEX IF NOT EXISTS ix_tareo_foto_hash ON ac_tareo_registro (foto_hash);

COMMIT;
