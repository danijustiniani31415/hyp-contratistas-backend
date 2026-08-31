-- ─────────────────────────────────────────────────────────────────────────────
-- Convocatoria recurrente por tema: un tema del catálogo (ej. "Reunión de
-- Jefaturas de Proyectos") puede tener un área/gerencia + puestos habituales
-- asociados, para sugerir participantes automáticamente al elegirlo.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS area_scope_id INT REFERENCES area_scope(area_scope_id);

CREATE TABLE IF NOT EXISTS reunion_tema_puesto (
    reunion_tema_puesto_id SERIAL PRIMARY KEY,
    reunion_tema_id          INT NOT NULL REFERENCES reunion_tema(reunion_tema_id),
    puesto_id                 INT NOT NULL REFERENCES puesto(puesto_id),
    created_date_time          TIMESTAMPTZ NOT NULL,
    created_user_id             INT NOT NULL,
    active                       BOOLEAN NOT NULL DEFAULT TRUE,
    state                        BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_reunion_tema_puesto_tema ON reunion_tema_puesto(reunion_tema_id);
