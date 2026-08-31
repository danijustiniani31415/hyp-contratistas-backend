-- ============================================================================
-- Backfill de workers.area_scope_id en las fichas de PRE-INGRESO
-- (workers_estado_id = 4 · FINALISTA_APROBADO).
--
-- Desde ahora, aprobar a un finalista le graba el área del solicitante que pidió
-- la vacante (gth_solicitud.area_scope_id) en su ficha de workers, para que la
-- programación de su EMO de ingreso pueda resolver su jefatura subiendo por el
-- árbol de area_scope. Este script hace lo mismo con los finalistas que ya
-- fueron aprobados antes del cambio y quedaron sin área.
--
-- Solo toca fichas con area_scope_id NULL: nunca reemplaza un área ya asignada.
-- Es idempotente (correrlo dos veces no cambia nada la segunda vez).
--
-- Ejecutar todo el bloque de una sola vez (es una única transacción).
-- Aplicado en DEV el 2026-08-21.
-- ============================================================================

BEGIN;

WITH pendientes AS (
    SELECT w.id AS worker_id, w.person_id
    FROM workers w
    WHERE w.workers_estado_id = 4
      AND w.area_scope_id IS NULL
      AND w.person_id IS NOT NULL
),
areas AS (
    -- Área del requerimiento en el que a esa persona la eligieron
    -- (gth_candidato_resultado_id = 4 · SELECCIONADO). Si pasó por más de un
    -- proceso, manda la decisión más reciente.
    SELECT p.worker_id, s.area_scope_id
    FROM pendientes p
    JOIN LATERAL (
        SELECT sol.area_scope_id
        FROM gth_postulante_formulario f
        JOIN gth_candidato c            ON c.gth_candidato_id = f.gth_candidato_id AND c.state
        JOIN gth_candidato_evaluacion e ON e.gth_candidato_id = c.gth_candidato_id AND e.state
                                       AND e.gth_candidato_resultado_id = 4
        JOIN gth_requerimiento req      ON req.gth_requerimiento_id = c.gth_requerimiento_id AND req.state
        JOIN gth_solicitud sol          ON sol.gth_solicitud_id = req.gth_solicitud_id AND sol.state
        WHERE f.person_id = p.person_id AND f.state AND sol.area_scope_id IS NOT NULL
        ORDER BY e.decision_date_time DESC NULLS LAST, req.gth_requerimiento_id DESC
        LIMIT 1
    ) s ON TRUE
)
UPDATE workers w
SET area_scope_id = a.area_scope_id,
    updated_at    = NOW()
FROM areas a
WHERE w.id = a.worker_id;

COMMIT;

-- Verificación: no deberían quedar fichas de pre-ingreso sin área cuya solicitud
-- sí tenga area_scope_id.
-- SELECT id, person_id, area_scope_id FROM workers WHERE workers_estado_id = 4;
