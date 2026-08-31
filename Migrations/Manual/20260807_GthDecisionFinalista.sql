-- ============================================================================
-- Gestión GTH · Reclutamiento — Decisión del solicitante sobre los finalistas
-- (RF-REC-24 y RF-REC-25 del requerimiento v2)
--
--  1) gth_candidato_resultado: se agregan SELECCIONADO (el solicitante lo aprobó
--     y pasa a onboarding) y RECHAZADO (el solicitante lo descartó y se le envió
--     el correo de agradecimiento). Los tres códigos previos no cambian.
--  2) gth_candidato_evaluacion: trazabilidad de la decisión del solicitante
--     (cuándo y quién). El correo de agradecimiento reusa las columnas
--     agradecimiento_* que ya existen.
--  3) gth_estado_requerimiento: nuevo estado final CERRADO — aprobar a un
--     finalista termina el proceso de reclutamiento y el seleccionado pasa al
--     proceso de onboarding (funcionalidad aparte, aún no implementada).
--  4) gth_correo_tipo: FINALISTA_DECISION — destinatarios de GTH a los que se
--     notifica la decisión final del solicitante (configurable desde la vista de
--     Solicitud de Personal, igual que LONG_LIST_DECISION).
--
-- Requiere haber corrido antes 20260807_GthEvaluacionFinalistas.sql.
-- Idempotente: se puede correr más de una vez.
-- ============================================================================

BEGIN;

-- ── 1) Resultados de la decisión del solicitante ────────────────────────────
INSERT INTO gth_candidato_resultado (codigo, nombre, orden)
VALUES
    ('SELECCIONADO', 'Seleccionado', 4),
    ('RECHAZADO',    'Rechazado',    5)
ON CONFLICT (codigo) WHERE state = true
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    orden             = EXCLUDED.orden,
    active            = true,
    updated_date_time = now();

-- ── 2) Trazabilidad de la decisión del solicitante ──────────────────────────
ALTER TABLE gth_candidato_evaluacion
    ADD COLUMN IF NOT EXISTS decision_date_time timestamptz NULL,
    ADD COLUMN IF NOT EXISTS decision_user_id   integer     NULL;

COMMENT ON COLUMN gth_candidato_evaluacion.decision_date_time IS
    'Momento en que el área solicitante aprobó o rechazó al finalista.';

-- ── 3) Estado final del requerimiento ───────────────────────────────────────
INSERT INTO gth_estado_requerimiento (codigo, nombre, descripcion, orden, created_date_time, active, state)
VALUES ('CERRADO', 'Cerrado',
        'El solicitante aprobó al finalista: el proceso de reclutamiento termina y el seleccionado pasa al proceso de onboarding.',
        12, now(), true, true)
ON CONFLICT (codigo) WHERE state = true
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    descripcion       = EXCLUDED.descripcion,
    orden             = EXCLUDED.orden,
    active            = true,
    updated_date_time = now();

-- ── 4) Tipo de correo de la decisión final (va a GTH) ───────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, created_date_time, active, state)
VALUES ('FINALISTA_DECISION', 'Decisión de finalista (a GTH)', now(), true, true)
ON CONFLICT (codigo) WHERE state = true
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    active            = true,
    updated_date_time = now();

COMMIT;
