-- ─────────────────────────────────────────────────────────────────────────────
-- Convocatoria recurrente por tema: pasa de soportar UNA sola área/gerencia +
-- puestos, a soportar VARIAS reglas independientes por tema (cada una con su
-- propia área/gerencia y/o proyecto + puestos), igual que ya permite el modal
-- de convocatoria masiva de participantes. Caso real: "Reunión de Jefaturas de
-- Proyectos" (Gerencia de Proyectos) que además debe convocar siempre al
-- Gerente Inmobiliario (Gerencia General) — hoy solo se puede una de las dos.
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS reunion_tema_regla (
    reunion_tema_regla_id  SERIAL PRIMARY KEY,
    reunion_tema_id          INT NOT NULL REFERENCES reunion_tema(reunion_tema_id),
    area_scope_id             INT REFERENCES area_scope(area_scope_id),
    project_id                 INT REFERENCES project(project_id),
    created_date_time           TIMESTAMPTZ NOT NULL,
    created_user_id                INT NOT NULL,
    updated_date_time                TIMESTAMPTZ,
    updated_user_id                     INT,
    active                                BOOLEAN NOT NULL DEFAULT TRUE,
    state                                 BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_reunion_tema_regla_tema ON reunion_tema_regla(reunion_tema_id);

-- Backfill: cada tema con área ya configurada pasa a tener una única regla inicial
-- (created_user_id = 1 = system/admin, ya que no hay un usuario real que atribuirle).
INSERT INTO reunion_tema_regla (reunion_tema_id, area_scope_id, project_id, created_date_time, created_user_id, active, state)
SELECT reunion_tema_id, area_scope_id, NULL, now(), 1, true, true
FROM reunion_tema
WHERE state = true;

ALTER TABLE reunion_tema_puesto ADD COLUMN IF NOT EXISTS reunion_tema_regla_id INT REFERENCES reunion_tema_regla(reunion_tema_regla_id);

-- Backfill: vincula cada puesto ya marcado a la regla recién creada de su mismo tema.
UPDATE reunion_tema_puesto p
SET reunion_tema_regla_id = r.reunion_tema_regla_id
FROM reunion_tema_regla r
WHERE r.reunion_tema_id = p.reunion_tema_id AND p.reunion_tema_regla_id IS NULL;

CREATE INDEX IF NOT EXISTS ix_reunion_tema_puesto_regla ON reunion_tema_puesto(reunion_tema_regla_id);

-- reunion_tema.area_scope_id y reunion_tema_puesto.reunion_tema_id quedan sin usarse en código
-- nuevo (reemplazados por reunion_tema_regla), pero se conservan por compatibilidad histórica.
