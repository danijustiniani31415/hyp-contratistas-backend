-- ============================================================================
-- 2026-08-07 · El residente del proyecto pasa de ser un correo suelto a una
--              referencia al trabajador.
--
-- Antes: project.email_residente era texto libre. Si el residente cambiaba de
-- correo, o se escribía mal, el correo de EMO salía sin él y nadie se enteraba.
--
-- Ahora: project.residente_workers_id → workers.id, y el correo se lee de
-- workers.email_corporativo al enviar. Así sigue siempre al dato maestro del
-- trabajador y deja de haber una segunda copia del correo que se desactualiza.
--
-- email_residente NO se elimina: se deja como columna histórica (la convención
-- del proyecto es no borrar campos, para auditoría). El código ya no la lee.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ============================================================================

BEGIN;

-- ── 1) Nueva columna ────────────────────────────────────────────────────────
ALTER TABLE project
    ADD COLUMN IF NOT EXISTS residente_workers_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_project_residente_workers'
    ) THEN
        ALTER TABLE project
            ADD CONSTRAINT fk_project_residente_workers
            FOREIGN KEY (residente_workers_id) REFERENCES workers (id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_project_residente_workers
    ON project (residente_workers_id) WHERE residente_workers_id IS NOT NULL;

-- ── 2) Migración de datos ───────────────────────────────────────────────────
-- Se cruza email_residente contra workers.email_corporativo (sin distinguir
-- mayúsculas ni espacios). Solo se migra cuando el cruce es inequívoco: si un
-- correo devolviera más de un trabajador se deja en NULL para resolverlo a mano
-- desde Configuración → Proyectos.
--
-- No se filtra por project.active ni por project.state: se migra todo lo que
-- tenga un correo cargado, incluidos los proyectos inactivos o cerrados, para
-- no perder el dato si alguno se reactiva.
UPDATE project pr
SET residente_workers_id = m.worker_id,
    updated_date_time    = now()
FROM (
    SELECT pr2.project_id,
           min(w.id)  AS worker_id,
           count(*)   AS coincidencias
    FROM project pr2
    JOIN workers w
      ON lower(btrim(w.email_corporativo)) = lower(btrim(pr2.email_residente))
    WHERE nullif(btrim(pr2.email_residente), '') IS NOT NULL
    GROUP BY pr2.project_id
    HAVING count(*) = 1
) m
WHERE pr.project_id = m.project_id
  AND pr.residente_workers_id IS NULL;

COMMIT;

-- ============================================================================
-- Verificación 1 — proyectos cuyo email_residente NO se pudo migrar.
-- Hay que asignarles el residente a mano desde Configuración → Proyectos, o
-- registrar antes a esa persona como trabajador con su correo corporativo.
--
-- SELECT pr.project_id,
--        pr.project_description AS proyecto,
--        pr.email_residente,
--        pr.active,
--        (SELECT count(*) FROM workers w
--          WHERE lower(btrim(w.email_corporativo)) = lower(btrim(pr.email_residente)))
--            AS trabajadores_que_coinciden
-- FROM project pr
-- WHERE nullif(btrim(pr.email_residente), '') IS NOT NULL
--   AND pr.residente_workers_id IS NULL
-- ORDER BY pr.project_description;
--
-- Verificación 2 — resultado de la migración.
--
-- SELECT pr.project_description AS proyecto,
--        pr.email_residente     AS correo_viejo,
--        p.full_name            AS residente,
--        w.email_corporativo    AS correo_nuevo
-- FROM project pr
-- JOIN workers w ON w.id = pr.residente_workers_id
-- LEFT JOIN person p ON p.person_id = w.person_id
-- ORDER BY pr.project_description;
-- ============================================================================
