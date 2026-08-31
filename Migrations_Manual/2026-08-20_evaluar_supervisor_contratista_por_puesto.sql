-- ============================================================================
-- Evaluar Supervisor de Contratista (Flujo A) — pasar de "tiene cuenta logueada
-- con rol de sistema 74" a "figura con puesto de campo de supervisor", incluya
-- o no cuenta en el sistema.
--
-- Antes: la lista de supervisores a evaluar salía de ss_contratista_usuario +
-- user_role (role_id=74), que exige que a alguien le hayan creado login Y
-- asignado el rol de sistema Y vinculado a un proyecto en
-- ss_contratista_usuario_proyecto. Diagnóstico del 2026-08-20: de 58 personas
-- con ese rol, 44 no tenían ningún proyecto vinculado ahí — quedaban invisibles
-- aunque sí tuvieran proyecto real en worker_vinculaciones.
--
-- Ahora: se toma de workers (contrata_casa='Contrata', puesto en la lista de
-- abajo) + worker_vinculaciones (proyecto vigente) — el mismo patrón que ya usa
-- DesempenoSupervisorFeature para "supervisor", sin depender de que la persona
-- tenga cuenta.
--
-- supervisor_ss_contratista_usuario_id pasa a ser NULLABLE y se agrega
-- supervisor_worker_id (nullable, FK a workers) — una evaluación queda con
-- exactamente una de las dos poblada. No se borra la columna vieja: nadie usó
-- este flujo todavía (recién habilitado hoy), pero por si acaso.
--
-- Idempotente.
-- ============================================================================

BEGIN;

ALTER TABLE ev_evaluacion_supervisor_contratista
    ALTER COLUMN supervisor_ss_contratista_usuario_id DROP NOT NULL;

ALTER TABLE ev_evaluacion_supervisor_contratista
    ADD COLUMN IF NOT EXISTS supervisor_worker_id integer REFERENCES workers(id);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_ev_eval_supervisor_origen'
    ) THEN
        ALTER TABLE ev_evaluacion_supervisor_contratista
            ADD CONSTRAINT ck_ev_eval_supervisor_origen CHECK (
                no_aplica = TRUE
                OR supervisor_ss_contratista_usuario_id IS NOT NULL
                OR supervisor_worker_id IS NOT NULL
            );
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_ev_eval_supervisor_worker
    ON ev_evaluacion_supervisor_contratista (supervisor_worker_id);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- Puestos de campo hoy considerados "supervisor" para este flujo (a pedido del
-- usuario, "por ahora a todos ellos"):
--   CAPATAZ, CAPATAZ SUPERVISOR DE CAMPO, SUPERVISOR, SUPERVISOR DE CAMPO,
--   PREVENCIONISTA, PREVENCIONISTA DE RIESGOS, SUPERVISOR DE ACABADOS,
--   ARQUITECTO SUPERVISOR DE CAMPO, INGENIERO DE PRODUCCION
--
-- SELECT count(*) AS candidatos
-- FROM workers w
-- JOIN puesto pu ON pu.puesto_id = w.puesto_id
-- JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
-- WHERE w.contrata_casa = 'Contrata'
--   AND upper(pu.nombre) IN (
--     'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
--     'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
--     'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
--   );
