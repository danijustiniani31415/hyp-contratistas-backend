-- ============================================================================
-- Gestión GTH · Reclutamiento — El candidato rechazado deja de estar "eliminado"
--
-- PROBLEMA
-- `gth_candidato.state` estaba haciendo dos trabajos a la vez. Al enviar una
-- long list nueva, `GuardarLongListCandidatos` daba de baja (state = false) la
-- long list anterior COMPLETA, sin distinguir dos casos muy distintos:
--
--   1. Corrección — GTH vuelve a subir la long list antes de que el solicitante
--      decida. Las filas anteriores son basura: state = false es correcto.
--   2. Vuelta nueva — el solicitante rechazó a todos, el requerimiento volvió a
--      LONG_LIST y GTH sube otra long list. Las filas anteriores NO están
--      borradas: son el historial de rechazados que el sistema debe mostrar.
--
-- En el proyecto `state = false` significa "eliminado del sistema, no mostrar
-- nunca", así que el caso 2 estaba mal modelado: un candidato RECHAZADO no está
-- eliminado. Su condición de rechazado ya vive donde corresponde, en
-- `gth_candidato_estado` (RECHAZADO) y en `gth_candidato_resultado` (NO_PASO /
-- RECHAZADO) para los rechazos de entrevista y decisión final.
--
-- SOLUCIÓN
-- `gth_candidato.numero_long_list`: a qué long list del requerimiento pertenece
-- el candidato (1 = la primera). A partir de ahora:
--
--   • Vuelta nueva  → los candidatos anteriores QUEDAN con state = true y la
--     long list nueva entra con numero_long_list + 1.
--   • Corrección    → las filas de la long list vigente sí se dan de baja
--     (state = false), porque ahí sí se están eliminando.
--
-- Las consultas de "long list vigente" pasan a filtrar por state = true Y por
-- la última vuelta, así que las anteriores no se cuelan en ninguna pantalla en
-- curso; el historial de rechazados lee todas las vueltas, siempre con
-- state = true.
--
-- Idempotente: ADD COLUMN IF NOT EXISTS e índice IF NOT EXISTS. Los dos UPDATE
-- de backfill son convergentes (se pueden repetir sin efectos raros).
-- ============================================================================

BEGIN;

-- ── 1. Columna con el número de long list ───────────────────────────────────

ALTER TABLE gth_candidato
    ADD COLUMN IF NOT EXISTS numero_long_list integer NOT NULL DEFAULT 1;

COMMENT ON COLUMN gth_candidato.numero_long_list IS
    'A que long list del requerimiento pertenece el candidato (1 = la primera). La vigente es el MAX entre las filas con state = true; las anteriores se conservan como historial de rechazados.';

-- ── 2. Rescate de las long lists que se dieron de baja siendo historial ─────
--
-- Un lote de carga = todas las filas que `GuardarLongListCandidatos` insertó de
-- una vez, y por eso comparten `created_date_time` exacto (el método captura un
-- único `now` para todo el lote). Se reactiva el lote entero cuando alguno de
-- sus candidatos llegó a tener decisión (estado ≠ PENDIENTE): eso significa que
-- el solicitante lo revisó y es historial, no una corrección.
--
-- Los lotes que quedaron 100 % en PENDIENTE sí eran correcciones y se quedan
-- como están (state = false), que es lo que corresponde.

WITH lotes AS (
    SELECT c.gth_requerimiento_id,
           c.created_date_time,
           bool_or(e.codigo <> 'PENDIENTE') AS decidido
      FROM gth_candidato c
      JOIN gth_candidato_estado e
        ON e.gth_candidato_estado_id = c.gth_candidato_estado_id
     GROUP BY c.gth_requerimiento_id, c.created_date_time
)
UPDATE gth_candidato c
   SET state             = true,
       updated_date_time = now()
  FROM lotes l
 WHERE l.gth_requerimiento_id = c.gth_requerimiento_id
   AND l.created_date_time    = c.created_date_time
   AND l.decidido
   AND c.state = false;

-- ── 3. Numerar la long list de cada candidato vivo ──────────────────────────
--
-- Un número por lote de carga, en orden cronológico. Solo cuentan los lotes que
-- quedaron vivos: las correcciones no son una vuelta del proceso y no deben
-- consumir un número (si no, el historial diría "long list 3" donde hubo 2).

WITH lotes AS (
    SELECT gth_requerimiento_id,
           created_date_time,
           DENSE_RANK() OVER (PARTITION BY gth_requerimiento_id
                                  ORDER BY created_date_time) AS numero
      FROM gth_candidato
     WHERE state = true
     GROUP BY gth_requerimiento_id, created_date_time
)
UPDATE gth_candidato c
   SET numero_long_list = l.numero
  FROM lotes l
 WHERE l.gth_requerimiento_id = c.gth_requerimiento_id
   AND l.created_date_time    = c.created_date_time
   AND c.state = true
   AND c.numero_long_list IS DISTINCT FROM l.numero;

-- ── 4. Índice para el "cuál es la vuelta vigente" ───────────────────────────
-- Las consultas resuelven la long list vigente con
-- MAX(numero_long_list) sobre las filas vivas del requerimiento.

CREATE INDEX IF NOT EXISTS ix_gth_candidato_req_vivo_vuelta
    ON gth_candidato (gth_requerimiento_id, numero_long_list)
    WHERE state = true;

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- Long lists por requerimiento, con cuántos candidatos y en qué estado quedaron:
--
-- SELECT c.gth_requerimiento_id AS req, c.numero_long_list AS long_list,
--        e.codigo AS estado, count(*) AS candidatos, bool_and(c.state) AS vivos
-- FROM gth_candidato c
-- JOIN gth_candidato_estado e ON e.gth_candidato_estado_id = c.gth_candidato_estado_id
-- GROUP BY 1, 2, 3
-- ORDER BY 1, 2, 3;
--
-- Esperado: ningún RECHAZADO con vivos = false (un rechazado nunca está
-- eliminado), y la numeración de cada requerimiento sin saltos (1, 2, 3…).
