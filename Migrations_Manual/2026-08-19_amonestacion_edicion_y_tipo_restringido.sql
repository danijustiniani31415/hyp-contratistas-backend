-- 1) Auditoría de corrección de amonestaciones (quién corrigió, cuándo)
ALTER TABLE ssoma_amonestaciones ADD COLUMN IF NOT EXISTS updated_by integer NULL;

-- 2) Distinguir el origen de un registro en la lista negra (ss_trabajador_restringido):
--    SANCION (retiro definitivo por amonestación) | DESCANSO_MEDICO (bloqueo temporal, no es
--    sanción) | MANUAL (agregado a mano desde la pestaña Inhabilitados).
ALTER TABLE ss_trabajador_restringido ADD COLUMN IF NOT EXISTS tipo varchar(30) NOT NULL DEFAULT 'MANUAL';

-- Backfill de los registros existentes según el patrón de su motivo actual,
-- para no dejar todo el histórico marcado como MANUAL por defecto.
UPDATE ss_trabajador_restringido
SET tipo = 'SANCION'
WHERE motivo ILIKE 'Retiro definitivo del proyecto%';

UPDATE ss_trabajador_restringido
SET tipo = 'DESCANSO_MEDICO'
WHERE motivo = 'Descanso médico aprobado';
