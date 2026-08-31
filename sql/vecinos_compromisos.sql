-- ============================================================================
-- Funcionalidad: Gestión de Vecinos — Compromisos y Entregables
-- Cada solicitud puede tener N compromisos; cada compromiso tiene 8 entregables.
-- Idempotente. Ejecutar después de vecinos_solicitudes.sql.
-- ============================================================================

BEGIN;

-- ── Catálogo: estado de compromiso ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_compromiso_estado (
    vecino_compromiso_estado_id serial PRIMARY KEY,
    descripcion                 varchar(50) NOT NULL,
    active                      boolean     NOT NULL DEFAULT true,
    state                       boolean     NOT NULL DEFAULT true
);

INSERT INTO vecino_compromiso_estado (descripcion)
SELECT v.descripcion
FROM (VALUES ('Pendiente'), ('En proceso'), ('Culminado')) AS v(descripcion)
WHERE NOT EXISTS (SELECT 1 FROM vecino_compromiso_estado e WHERE e.descripcion = v.descripcion);

-- ── Catálogo: tipo de entregable (los 8 ítems) ──────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_entregable_tipo (
    vecino_entregable_tipo_id serial PRIMARY KEY,
    descripcion               varchar(100) NOT NULL,
    orden                     int          NOT NULL DEFAULT 0,
    active                    boolean      NOT NULL DEFAULT true,
    state                     boolean      NOT NULL DEFAULT true
);

INSERT INTO vecino_entregable_tipo (descripcion, orden)
SELECT v.descripcion, v.orden
FROM (VALUES
    ('Acta de Compromiso', 1),
    ('Aprobación de Gerencia o Residente', 2),
    ('Aprobación de Vecino', 3),
    ('Evidencia antes', 4),
    ('Evidencia después', 5),
    ('Evidencia durante', 6),
    ('Acta de Conformidad Parcial', 7),
    ('Acta de Conformidad Final', 8)
) AS v(descripcion, orden)
WHERE NOT EXISTS (SELECT 1 FROM vecino_entregable_tipo t WHERE t.descripcion = v.descripcion);

-- ── Catálogo: estado de entregable ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_entregable_estado (
    vecino_entregable_estado_id serial PRIMARY KEY,
    descripcion                 varchar(50) NOT NULL,
    active                      boolean     NOT NULL DEFAULT true,
    state                       boolean     NOT NULL DEFAULT true
);

INSERT INTO vecino_entregable_estado (descripcion)
SELECT v.descripcion
FROM (VALUES ('Falta'), ('Enviado'), ('Aprobado'), ('No aplica')) AS v(descripcion)
WHERE NOT EXISTS (SELECT 1 FROM vecino_entregable_estado e WHERE e.descripcion = v.descripcion);

-- ── Tabla: vecino_compromiso ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_compromiso (
    vecino_compromiso_id        serial PRIMARY KEY,
    vecino_solicitud_id         int           NOT NULL REFERENCES vecino_solicitud(vecino_solicitud_id),
    descripcion                 varchar(1000) NOT NULL,
    es_critico                  boolean       NOT NULL DEFAULT false,
    vecino_compromiso_estado_id int           NOT NULL REFERENCES vecino_compromiso_estado(vecino_compromiso_estado_id),
    fecha_inicio                date,
    fecha_fin                   date,
    created_date_time           timestamp     NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    created_user_id             int           NOT NULL REFERENCES app_user(user_id),
    updated_date_time           timestamp,
    updated_user_id             int           REFERENCES app_user(user_id),
    active                      boolean       NOT NULL DEFAULT true,
    state                       boolean       NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_vecino_compromiso_solicitud_id ON vecino_compromiso(vecino_solicitud_id);

-- ── Tabla: vecino_compromiso_entregable ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_compromiso_entregable (
    vecino_compromiso_entregable_id serial PRIMARY KEY,
    vecino_compromiso_id            int NOT NULL REFERENCES vecino_compromiso(vecino_compromiso_id),
    vecino_entregable_tipo_id       int NOT NULL REFERENCES vecino_entregable_tipo(vecino_entregable_tipo_id),
    vecino_entregable_estado_id     int NOT NULL REFERENCES vecino_entregable_estado(vecino_entregable_estado_id),
    created_date_time               timestamp NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    created_user_id                 int       NOT NULL REFERENCES app_user(user_id),
    updated_date_time               timestamp,
    updated_user_id                 int       REFERENCES app_user(user_id),
    active                          boolean   NOT NULL DEFAULT true,
    state                           boolean   NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_vecino_compromiso_entregable_compromiso_id ON vecino_compromiso_entregable(vecino_compromiso_id);

COMMIT;
