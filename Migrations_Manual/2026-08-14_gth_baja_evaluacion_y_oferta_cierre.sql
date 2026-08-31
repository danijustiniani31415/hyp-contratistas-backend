-- ============================================================================
-- Gestión GTH · Reclutamiento — Baja de las fases "Evaluación" y "Oferta y cierre"
--
-- GTH pidió sacar del pipeline las fases EVALUACION y OFERTA_CIERRE: ninguna de
-- las dos correspondía a un paso real del proceso implementado. Lo que hacía
-- "Evaluación" (cargar el resultado psicotécnico y técnico del candidato) hoy es
-- parte del informe que GTH registra dentro de Entrevistas, y la carta oferta
-- de "Oferta y cierre" no se gestiona en el sistema: al aprobar al finalista el
-- requerimiento pasa directo a Cerrado.
--
-- A cambio, SELECCION_JEFATURA deja de ser la fase heredada que nadie usaba y
-- pasa a ser la fase real que sigue a Entrevistas: cuando GTH envía a los
-- finalistas con su informe, el requerimiento queda ahí esperando la decisión
-- del área solicitante (antes se quedaba en Entrevistas, que era engañoso: GTH
-- ya había terminado su parte).
--
-- El pipeline queda en 10 fases:
--   1 Nuevo / Solicitud          6 Long list enviada
--   2 Aprobación Gerencia Gral.  7 Long list aprobada
--   3 Validación GTH             8 Entrevistas
--   4 Publicación de vacante     9 Selección jefatura
--   5 Long list / CVs           10 Cerrado
-- (+ Rechazado por Gerencia General, que sigue con active = false porque es un
--  estado del requerimiento y no un paso del pipeline.)
--
-- Cuatro cambios:
--
-- 1. Los requerimientos que estuvieran parados en una de las dos fases que se
--    van se mueven a ENTREVISTAS. Es defensivo: ningún flujo del backend movía
--    un requerimiento a esas fases, así que no debería haber ninguno, pero si
--    alguien lo hizo a mano no puede quedar apuntando a una fase eliminada.
--
-- 2. Los requerimientos que están en ENTREVISTAS y ya tienen finalistas
--    enviados (candidato vigente con evaluación en PASO) pasan a
--    SELECCION_JEFATURA: es la fase en la que el código nuevo espera
--    encontrarlos para dejar decidir al solicitante, así que sin esto los
--    procesos en curso se quedarían sin tarjeta de decisión.
--
-- 3. Baja lógica de EVALUACION y OFERTA_CIERRE (state = false, o sea eliminado)
--    y renumeración de `orden` para que quede contiguo: el seguimiento vertical
--    del requerimiento pinta ese número en el círculo de cada fase, así que un
--    hueco se vería como un "10, 12" en una lista de 10 pasos.
--
-- 4. Descripción de ENTREVISTAS y SELECCION_JEFATURA: las dos describían un
--    reparto de trabajo que ya no es el real (Entrevistas decía que ahí se
--    selecciona al candidato final, y Selección jefatura describía la revisión
--    de CVs, que es lo que hacen Long list enviada/aprobada).
--
-- Idempotente: se puede correr varias veces. El paso 1 y el 2 no encuentran nada
-- en la segunda corrida, y el 3 y el 4 son asignaciones a un valor fijo.
-- ============================================================================

BEGIN;

-- ── 1. Requerimientos parados en las fases que se eliminan ──────────────────

UPDATE gth_requerimiento r
   SET gth_estado_requerimiento_id = (SELECT gth_estado_requerimiento_id
                                        FROM gth_estado_requerimiento
                                       WHERE codigo = 'ENTREVISTAS' AND state = true),
       updated_date_time           = now()
 WHERE r.gth_estado_requerimiento_id IN (
           SELECT gth_estado_requerimiento_id
             FROM gth_estado_requerimiento
            WHERE codigo IN ('EVALUACION', 'OFERTA_CIERRE'));

-- ── 2. En Entrevistas con finalistas ya enviados → Selección jefatura ───────
-- "Finalista enviado" = candidato vivo del requerimiento cuya evaluación vigente
-- quedó en PASO, que es la condición con la que el backend arma la tarjeta
-- "Finalistas enviados por GTH" del solicitante.
--
-- A propósito no se filtra además por la última vuelta de long list
-- (`numero_long_list`, como sí hace el repositorio): un PASO de una vuelta
-- anterior no existe —al rechazar a todos los finalistas quedan en RECHAZADO y
-- el requerimiento sale de Entrevistas—, y así este script no depende de que la
-- migración de `numero_long_list` ya esté aplicada en el entorno.

UPDATE gth_requerimiento r
   SET gth_estado_requerimiento_id = (SELECT gth_estado_requerimiento_id
                                        FROM gth_estado_requerimiento
                                       WHERE codigo = 'SELECCION_JEFATURA' AND state = true),
       updated_date_time           = now()
 WHERE r.state = true
   AND r.gth_estado_requerimiento_id = (SELECT gth_estado_requerimiento_id
                                          FROM gth_estado_requerimiento
                                         WHERE codigo = 'ENTREVISTAS' AND state = true)
   AND EXISTS (
           SELECT 1
             FROM gth_candidato c
             JOIN gth_candidato_evaluacion ev ON ev.gth_candidato_id = c.gth_candidato_id
             JOIN gth_candidato_resultado  res ON res.gth_candidato_resultado_id = ev.gth_candidato_resultado_id
            WHERE c.gth_requerimiento_id = r.gth_requerimiento_id
              AND c.state = true AND ev.state = true
              AND res.codigo = 'PASO');

-- ── 3. Baja lógica de las dos fases + renumeración del pipeline ─────────────
-- state = false es la baja (eliminado); active = false además las deja fuera de
-- cualquier filtro/desplegable. Se conserva la fila para auditoría, con su
-- `orden` original, que ya no significa nada al estar fuera del pipeline.

UPDATE gth_estado_requerimiento
   SET state             = false,
       active            = false,
       updated_date_time = now()
 WHERE codigo IN ('EVALUACION', 'OFERTA_CIERRE')
   AND state = true;

-- El orden nuevo se escribe explícito (y no con un row_number sobre el actual)
-- para que el pipeline quede fijado acá y no dependa de cómo estaba antes.
UPDATE gth_estado_requerimiento e
   SET orden             = v.orden,
       updated_date_time = now()
  FROM (VALUES
            ('NUEVO',               1),
            ('APROBACION_GG',       2),
            ('VALIDACION_GTH',      3),
            ('PUBLICACION',         4),
            ('LONG_LIST',           5),
            ('LONG_LIST_ENVIADA',   6),
            ('LONG_LIST_APROBADA',  7),
            ('ENTREVISTAS',         8),
            ('SELECCION_JEFATURA',  9),
            ('CERRADO',            10),
            -- Fuera del pipeline (active = false), pero su orden tiene que
            -- seguir siendo mayor que el de todas las fases: el seguimiento lo
            -- usa para saber que un rechazo del GG no cumplió ninguna fase.
            ('RECHAZADO_GG',       11)
       ) AS v(codigo, orden)
 WHERE e.codigo = v.codigo
   AND e.state = true
   AND e.orden <> v.orden;

-- ── 4. Descripciones de las dos fases que cambiaron de significado ──────────

UPDATE gth_estado_requerimiento
   SET descripcion       = 'GTH programa las entrevistas, registra el informe de cada candidato y envía a los finalistas al área solicitante.',
       updated_date_time = now()
 WHERE codigo = 'ENTREVISTAS' AND state = true;

UPDATE gth_estado_requerimiento
   SET descripcion       = 'GTH ya envió a los finalistas con su informe; el área solicitante revisa y elige a quién le da el puesto.',
       updated_date_time = now()
 WHERE codigo = 'SELECCION_JEFATURA' AND state = true;

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT orden, codigo, nombre, active, state
-- FROM gth_estado_requerimiento ORDER BY state DESC, orden;
-- Esperado: 10 fases activas con orden 1..10 (Nuevo → Cerrado), RECHAZADO_GG en
-- orden 11 con active = false, y EVALUACION / OFERTA_CIERRE con state = false.
--
-- SELECT e.codigo, count(*) AS reqs
-- FROM gth_requerimiento r
-- JOIN gth_estado_requerimiento e USING (gth_estado_requerimiento_id)
-- GROUP BY e.codigo ORDER BY 2 DESC;
-- Esperado: ningún requerimiento en EVALUACION ni en OFERTA_CIERRE.
