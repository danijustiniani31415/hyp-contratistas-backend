-- ============================================================================
-- Funcionalidad: Gestión de Vecinos — Solicitudes
-- Crea el catálogo de estados de solicitud y la tabla vecino_solicitud.
-- Idempotente. Ejecutar después de vecinos_gestion.sql.
-- ============================================================================

BEGIN;

-- ── Catálogo: estado de solicitud ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_solicitud_estado (
    vecino_solicitud_estado_id serial PRIMARY KEY,
    descripcion                varchar(50) NOT NULL,
    active                     boolean     NOT NULL DEFAULT true,
    state                      boolean     NOT NULL DEFAULT true
);

INSERT INTO vecino_solicitud_estado (descripcion)
SELECT v.descripcion
FROM (VALUES ('Por responder'), ('Aceptada'), ('Denegada')) AS v(descripcion)
WHERE NOT EXISTS (
    SELECT 1 FROM vecino_solicitud_estado e WHERE e.descripcion = v.descripcion
);

-- ── Tabla: vecino_solicitud ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_solicitud (
    vecino_solicitud_id        serial PRIMARY KEY,
    vecino_id                  int          NOT NULL REFERENCES vecino(vecino_id),
    descripcion                varchar(1000) NOT NULL,
    es_critica                 boolean      NOT NULL DEFAULT false,
    vecino_solicitud_estado_id int          NOT NULL REFERENCES vecino_solicitud_estado(vecino_solicitud_estado_id),
    created_date_time          timestamp    NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    created_user_id            int          NOT NULL REFERENCES app_user(user_id),
    updated_date_time          timestamp,
    updated_user_id            int          REFERENCES app_user(user_id),
    active                     boolean      NOT NULL DEFAULT true,
    state                      boolean      NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_vecino_solicitud_vecino_id ON vecino_solicitud(vecino_id);

COMMIT;
