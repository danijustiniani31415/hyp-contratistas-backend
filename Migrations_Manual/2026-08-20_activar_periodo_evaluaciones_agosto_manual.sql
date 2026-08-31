-- ============================================================================
-- Activación manual del período de evaluaciones de Agosto 2026.
--
-- Replica EXACTAMENTE lo que SincronizarVigenciaAsync() (EvPeriodoRepository.cs)
-- crearía automáticamente el día 25/08 vía el cron
-- (GET /api/v1/evaluaciones/recordatorios/procesar-diario):
--   - ciclo: apertura 25/08/2026, cierre 04/09/2026
--   - un solo período activo a la vez (se desactiva cualquier otro)
--
-- Esto NO reemplaza el cron real; es solo para poder ver los 3 flujos SSOMA
-- (y Residentes/Contratistas, que comparten la misma tabla ev_periodo)
-- funcionando en desarrollo antes de que llegue el 25.
--
-- Idempotente: se puede re-correr sin duplicar filas.
-- ============================================================================

BEGIN;

INSERT INTO ev_periodo (mes, anio, fecha_apertura, fecha_cierre, activo)
SELECT 8, 2026, DATE '2026-08-25', DATE '2026-09-04', true
WHERE NOT EXISTS (SELECT 1 FROM ev_periodo WHERE mes = 8 AND anio = 2026);

UPDATE ev_periodo
SET fecha_apertura = DATE '2026-08-25',
    fecha_cierre   = DATE '2026-09-04',
    activo         = true
WHERE mes = 8 AND anio = 2026;

UPDATE ev_periodo
SET activo = false
WHERE NOT (mes = 8 AND anio = 2026) AND activo = true;

COMMIT;

-- Verificación (no modifica nada):
-- SELECT id, mes, anio, fecha_apertura, fecha_cierre, activo FROM ev_periodo WHERE activo = true;
