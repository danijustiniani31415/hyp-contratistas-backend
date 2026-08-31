-- ============================================================================
-- Gestión GTH · Reclutamiento — El requerimiento cierra con el EMO APTO
--
-- Antes el proceso pasaba a CERRADO apenas GTH agendaba el examen médico de
-- ingreso del finalista. Ahora lo cierra el RESULTADO del examen: "Apto" o
-- "Apto con Restricciones". Con "No Apto" el proceso vuelve a manos de GTH,
-- que continúa con un candidato del historial de rechazados o prepara una
-- nueva long list. "Observado" no mueve nada: la aptitud final la define la
-- interconsulta.
--
-- 1) `gth_estado_requerimiento`: nueva fase EMO_NO_APTO.
--    Va con active = false, igual que RECHAZADO_GG: es un estado del
--    requerimiento y no un paso por el que pasen todos los procesos, y la
--    línea de tiempo del seguimiento solo lista las fases activas. El orden 10
--    (el mismo de EMO_INGRESO) es lo que hace que esa línea marque el paso del
--    EMO como el actual en vez de dar todo el pipeline por cumplido.
--
-- 2) `gth_candidato_resultado`: nuevo resultado NO_APTO_EMO.
--    Es el seleccionado que no pasó el examen. Necesita resultado propio: si se
--    lo dejara en RECHAZADO, el historial diría que lo descartó el área
--    solicitante en la decisión final, que es lo contrario de lo que pasó — el
--    área lo eligió y lo frenó el examen médico.
--
-- No hay backfill: los requerimientos ya cerrados se quedan como están (su
-- gente ya firmó o está en onboarding) y la regla nueva aplica desde el
-- próximo EMO de ingreso que se registre.
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

-- 1) Fase «EMO no apto» ------------------------------------------------------
INSERT INTO gth_estado_requerimiento (codigo, nombre, orden, descripcion, active, state)
SELECT 'EMO_NO_APTO', 'EMO no apto', 10,
       'El examen médico de ingreso del finalista salió No Apto. GTH retoma el proceso: continúa con un candidato del historial de rechazados o prepara una nueva long list.',
       false, true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_estado_requerimiento WHERE codigo = 'EMO_NO_APTO' AND state = true
);

-- 2) Resultado «No apto (EMO)» del candidato --------------------------------
INSERT INTO gth_candidato_resultado (codigo, nombre, orden, active, state)
SELECT 'NO_APTO_EMO', 'No apto (EMO)', 6, true, true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_candidato_resultado WHERE codigo = 'NO_APTO_EMO' AND state = true
);

COMMIT;

-- Verificación ---------------------------------------------------------------
-- SELECT codigo, nombre, orden, active, state FROM gth_estado_requerimiento ORDER BY orden;
-- SELECT codigo, nombre, orden, active, state FROM gth_candidato_resultado  ORDER BY orden;
