-- ============================================================================
-- Gestión GTH · Reclutamiento — Baja de los puntajes de la evaluación
--
-- El área usuaria pidió que los cuatro porcentajes de la entrevista (entrevista,
-- psicotécnico, técnica y resultado) dejen de registrarse en GTH y dejen de
-- mostrarse en el informe de finalistas que ve el área solicitante. El informe
-- se queda solo con los tres comentarios (resultado de entrevista, informe
-- psicotécnico y recomendación GTH).
--
-- Se eliminan de gth_candidato_evaluacion:
--   · el CHECK que validaba los rangos 0-100 (depende de las cuatro columnas),
--   · las cuatro columnas de puntaje.
--
-- Ojo: al quedarse sin puntaje_resultado, los finalistas ya no se ordenan por
-- puntaje sino alfabéticamente (ver ReclutamientoRepository.GetRevisionFinalistas).
--
-- Idempotente: se puede correr más de una vez.
-- ============================================================================

BEGIN;

ALTER TABLE gth_candidato_evaluacion
    DROP CONSTRAINT IF EXISTS ck_gth_candidato_evaluacion_puntajes;

ALTER TABLE gth_candidato_evaluacion
    DROP COLUMN IF EXISTS puntaje_entrevista,
    DROP COLUMN IF EXISTS puntaje_psicotecnico,
    DROP COLUMN IF EXISTS puntaje_tecnica,
    DROP COLUMN IF EXISTS puntaje_resultado;

COMMENT ON TABLE gth_candidato_evaluacion IS
    'Evaluación de la entrevista de un candidato: comentarios del informe, resultado y correo de agradecimiento cuando no continúa.';

COMMIT;
