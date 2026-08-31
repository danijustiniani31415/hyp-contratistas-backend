-- ============================================================================
-- Remodelado Vecinos: separar LOTE (edificio) de VECINO (departamento).
--   * Nueva tabla vecino_lote (dirección + observaciones a nivel de lote).
--   * vecino.vecino_lote_id  -> lote al que pertenece el departamento.
--   * project_croquis_lote.vecino_lote_id -> lote que representa el polígono.
--   * Backfill 1:1: cada vecino existente genera su propio lote (los datos
--     actuales son 1 lote = 1 vecino), y cada polígono se enlaza al lote de su
--     vecino previamente asignado.
--   * Se conservan (obsoletas) las columnas vecino.direccion / vecino.observaciones
--     y project_croquis_lote.vecino_id por auditoría.
-- Idempotente: puede reejecutarse sin duplicar datos.
-- ============================================================================

BEGIN;

-- ── 1. Tabla vecino_lote ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_lote (
    vecino_lote_id    serial PRIMARY KEY,
    project_id        int       NOT NULL REFERENCES project(project_id),
    direccion         varchar(250),
    observaciones     text,
    created_date_time timestamp NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    created_user_id   int       NOT NULL REFERENCES app_user(user_id),
    updated_date_time timestamp,
    updated_user_id   int       REFERENCES app_user(user_id),
    active            boolean   NOT NULL DEFAULT true,
    state             boolean   NOT NULL DEFAULT true
);
CREATE INDEX IF NOT EXISTS ix_vecino_lote_project_id ON vecino_lote(project_id);

-- ── 2. FKs vecino_lote_id ────────────────────────────────────────────────────
ALTER TABLE vecino
    ADD COLUMN IF NOT EXISTS vecino_lote_id int REFERENCES vecino_lote(vecino_lote_id);
ALTER TABLE project_croquis_lote
    ADD COLUMN IF NOT EXISTS vecino_lote_id int REFERENCES vecino_lote(vecino_lote_id);

CREATE INDEX IF NOT EXISTS ix_vecino_vecino_lote_id ON vecino(vecino_lote_id);
CREATE INDEX IF NOT EXISTS ix_project_croquis_lote_vecino_lote_id ON project_croquis_lote(vecino_lote_id);

-- La dirección deja de ser obligatoria en vecino (vive ahora en vecino_lote).
ALTER TABLE vecino ALTER COLUMN direccion DROP NOT NULL;

-- ── 3. Backfill 1:1: cada vecino sin lote genera su propio lote ──────────────
DO $$
DECLARE
    r      RECORD;
    new_id int;
BEGIN
    FOR r IN SELECT * FROM vecino WHERE vecino_lote_id IS NULL LOOP
        INSERT INTO vecino_lote (project_id, direccion, observaciones,
                                 created_date_time, created_user_id,
                                 updated_date_time, updated_user_id, active, state)
        VALUES (r.project_id, r.direccion, r.observaciones,
                r.created_date_time, r.created_user_id,
                r.updated_date_time, r.updated_user_id, r.active, r.state)
        RETURNING vecino_lote_id INTO new_id;

        UPDATE vecino SET vecino_lote_id = new_id WHERE vecino_id = r.vecino_id;
    END LOOP;
END $$;

-- ── 4. Enlazar polígonos del croquis al lote de su vecino previamente asignado ─
UPDATE project_croquis_lote pcl
SET    vecino_lote_id = v.vecino_lote_id
FROM   vecino v
WHERE  pcl.vecino_id = v.vecino_id
  AND  pcl.vecino_id IS NOT NULL
  AND  pcl.vecino_lote_id IS NULL;

-- ── 5. vecino.vecino_lote_id pasa a NOT NULL si ya no quedan nulos ───────────
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM vecino WHERE vecino_lote_id IS NULL) THEN
        ALTER TABLE vecino ALTER COLUMN vecino_lote_id SET NOT NULL;
    END IF;
END $$;

COMMIT;
