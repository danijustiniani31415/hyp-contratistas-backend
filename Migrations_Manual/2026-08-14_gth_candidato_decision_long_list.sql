-- ============================================================================
-- Gestión GTH · Reclutamiento — Historial de candidatos rechazados
--
-- El detalle del requerimiento (vista de GTH) y el «Estado del reclutamiento»
-- (vista del solicitante) ahora listan a TODOS los candidatos que quedaron
-- rechazados a lo largo del proceso, con la etapa en la que se los rechazó:
--
--   • Long list      — el área solicitante los rechazó al revisar los CVs.
--   • Entrevistas    — GTH los descartó tras la entrevista (correo de
--                      agradecimiento; resultado NO_PASO).
--   • Decisión final — el área solicitante rechazó al finalista (resultado
--                      RECHAZADO de `gth_candidato_evaluacion`).
--
-- La etapa NO se guarda: se deriva de los catálogos que ya existen
-- (`gth_candidato_estado` + `gth_candidato_resultado`), así que no puede
-- desincronizarse de la decisión real.
--
-- Lo que sí faltaba era CUÁNDO rechazó el solicitante a un candidato de la long
-- list. Eso vivía en `updated_date_time`, pero al enviar una long list nueva
-- (justo lo que pasa cuando se rechaza a todos) se da de baja la anterior con
-- `updated_date_time = now()` y la fecha del rechazo se pierde — que es
-- precisamente el caso que el historial tiene que mostrar. Por eso se agregan
-- las dos columnas de la decisión, con el mismo nombre que ya usa
-- `gth_candidato_evaluacion` para la decisión final del solicitante.
--
-- Los candidatos ya decididos antes de este cambio quedan con la columna en
-- null: el backend cae a `updated_date_time` (y luego a `created_date_time`)
-- para no dejar filas sin fecha en el historial.
--
-- Idempotente: ADD COLUMN IF NOT EXISTS.
-- ============================================================================

BEGIN;

ALTER TABLE gth_candidato
    ADD COLUMN IF NOT EXISTS decision_date_time timestamptz NULL,
    ADD COLUMN IF NOT EXISTS decision_user_id   integer     NULL;

COMMENT ON COLUMN gth_candidato.decision_date_time IS
    'Momento en que el solicitante aprobo/rechazo al candidato en la revision de la long list (UTC). Null si aun no lo decidio.';

COMMENT ON COLUMN gth_candidato.decision_user_id IS
    'Usuario del area solicitante que decidio sobre el candidato en la long list.';

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT column_name, is_nullable, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'gth_candidato'
--   AND column_name IN ('decision_date_time', 'decision_user_id')
-- ORDER BY column_name;
-- Esperado: decision_date_time | YES | timestamp with time zone
--           decision_user_id   | YES | integer
