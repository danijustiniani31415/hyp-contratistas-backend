-- ============================================================================
-- Gestión GTH · Reclutamiento — gth_requerimiento.gth_puesto_id pasa a NULLABLE
--
-- Arreglo de un pendiente de `categoria_puesto_unificados.sql`: esa migración
-- movió el puesto del requerimiento de `gth_puesto_id` (catálogo gth_puesto) a
-- `puesto_id` (catálogo `puesto` unificado), dejó `puesto_id` NOT NULL y el
-- modelo de EF ya solo mapea esa columna nueva… pero `gth_puesto_id` se quedó
-- NOT NULL. Resultado: todo INSERT en gth_requerimiento (registrar una solicitud
-- de personal) fallaba con
--
--   23502: el valor nulo en la columna «gth_puesto_id» ... viola la restricción
--   de no nulo
--
-- porque EF ya no manda esa columna. No se detectó antes porque después de la
-- unificación no se había registrado ninguna solicitud nueva.
--
-- La columna NO se elimina (regla del proyecto: ningún campo se borra, queda
-- para auditoría): los requerimientos anteriores conservan a qué gth_puesto
-- apuntaban. Solo se relaja el NOT NULL para que las filas nuevas puedan venir
-- sin ella. La FK a gth_puesto se mantiene y NULL la satisface.
--
-- Idempotente: se puede correr múltiples veces (DROP NOT NULL sobre una columna
-- que ya lo permite no hace nada).
-- ============================================================================

BEGIN;

ALTER TABLE gth_requerimiento ALTER COLUMN gth_puesto_id DROP NOT NULL;

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT column_name, is_nullable
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'gth_requerimiento'
--   AND column_name IN ('gth_puesto_id', 'puesto_id');
-- Esperado: gth_puesto_id = YES, puesto_id = NO
