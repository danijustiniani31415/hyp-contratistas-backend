-- Ejecutar en desarrollo primero, luego en producción.
--
-- ssoma_inspeccion guardaba al inspector SOLO como texto (inspector_nombre). Desempeño
-- Supervisor atribuía la inspección comparando ese texto contra el nombre actual del
-- trabajador, así que al corregir un nombre en la ficha (una tilde, una coma, "Yuly" por
-- "July") los registros viejos quedaban sin dueño y los porcentajes de meses YA CERRADOS
-- bajaban solos (incidencia Corilla, ago-2026).
--
-- Se agrega la FK al worker, que es inmune a los cambios de nombre. ssoma_opt ya tenía su
-- equivalente (observador_id, 97% poblado) y ssoma_rac tiene reportante_id/created_by, así
-- que esta es la única tabla que necesitaba la columna.
--
-- El backfill replica la normalización de NombreSupervisorMatcher (mayúsculas, sin tildes,
-- sin puntuación, tokens sin orden) en dos pasos por rendimiento: primero igualdad de clave
-- canónica contra todos los workers (hash join, instantáneo) y después contención parcial
-- —nombre abreviado contra completo— solo contra el staff, que es un conjunto chico. Nunca
-- se atribuye cuando el nombre calza con más de una persona: adivinar es peor que no contar.

BEGIN;

-- ── 1. Columna + FK ─────────────────────────────────────────────────────────────
ALTER TABLE ssoma_inspeccion
    ADD COLUMN IF NOT EXISTS inspector_worker_id INTEGER NULL REFERENCES workers(id);

CREATE INDEX IF NOT EXISTS ix_ssoma_inspeccion_inspector_worker
    ON ssoma_inspeccion (inspector_worker_id, fecha);

-- ── 2. Backfill por created_by (el usuario que la creó → su worker) ─────────────
-- La vía más firme del histórico.
UPDATE ssoma_inspeccion i
SET    inspector_worker_id = w.id
FROM   person p
JOIN   workers w ON w.person_id = p.person_id
WHERE  i.inspector_worker_id IS NULL
  AND  i.created_by IS NOT NULL
  AND  p.user_id = i.created_by;

-- ── 3. Backfill por nombre: clave canónica (tokens normalizados y ordenados) ────
CREATE TEMP TABLE tmp_sup_canon ON COMMIT DROP AS
SELECT w.id AS worker_id,
       w.obra_oficina_staff_id,
       array(SELECT DISTINCT tk FROM unnest(
           array_remove(string_to_array(
               regexp_replace(
                   upper(translate(
                       coalesce(p.full_name, trim(coalesce(p.first_names,'') || ' ' || coalesce(p.first_last_name,''))),
                       'ÁÉÍÓÚÀÈÌÒÙÄËÏÖÜÑáéíóúàèìòùñ', 'AEIOUAEIOUAEIOUNaeiouaeioun')),
                   '[^A-Za-z0-9 ]', ' ', 'g'), ' '), '')) tk ORDER BY tk) AS tokens
FROM   workers w
JOIN   person p ON p.person_id = w.person_id
WHERE  w.estado = 'ACTIVO';

CREATE TEMP TABLE tmp_insp_canon ON COMMIT DROP AS
SELECT id,
       array(SELECT DISTINCT tk FROM unnest(
           array_remove(string_to_array(
               regexp_replace(
                   upper(translate(inspector_nombre,
                                   'ÁÉÍÓÚÀÈÌÒÙÄËÏÖÜÑáéíóúàèìòùñ',
                                   'AEIOUAEIOUAEIOUNaeiouaeioun')),
                   '[^A-Za-z0-9 ]', ' ', 'g'), ' '), '')) tk ORDER BY tk) AS tokens
FROM   ssoma_inspeccion
WHERE  inspector_worker_id IS NULL
  AND  inspector_nombre IS NOT NULL;

-- 3a. Mismo conjunto de tokens (resuelve tildes, comas, orden de nombre/apellido).
--     Se agrupa por clave para descartar homónimos exactos.
WITH sup_unica AS (
    SELECT tokens, min(worker_id) AS worker_id
    FROM   tmp_sup_canon
    WHERE  array_length(tokens, 1) >= 1
    GROUP BY tokens
    HAVING count(DISTINCT worker_id) = 1
)
UPDATE ssoma_inspeccion i
SET    inspector_worker_id = s.worker_id
FROM   tmp_insp_canon c
JOIN   sup_unica s ON s.tokens = c.tokens
WHERE  i.id = c.id
  AND  i.inspector_worker_id IS NULL;

-- 3b. Nombre abreviado contra nombre completo, solo contra el staff (conjunto chico) y
--     con 3+ tokens a ambos lados: es donde el match parcial deja de ser adivinanza.
WITH candidatos AS (
    SELECT c.id,
           min(s.worker_id)            AS worker_id,
           count(DISTINCT s.worker_id) AS n
    FROM   tmp_insp_canon c
    JOIN   tmp_sup_canon s
      ON   s.obra_oficina_staff_id IS NOT NULL
     AND   array_length(c.tokens, 1) >= 3
     AND   array_length(s.tokens, 1) >= 3
     AND   (c.tokens <@ s.tokens OR s.tokens <@ c.tokens)
    GROUP BY c.id
)
UPDATE ssoma_inspeccion i
SET    inspector_worker_id = k.worker_id
FROM   candidatos k
WHERE  i.id = k.id
  AND  k.n = 1
  AND  i.inspector_worker_id IS NULL;

-- ── 4. Los que la normalización no puede resolver (cambio real de letras) ───────
-- Verificados uno por uno: son nombres únicos en la base, sin homónimos.
--   "CORILLA ROMERO, YULY DANIELA"  -> CORILLA ROMERO JULY DANIELA         (worker 13330)
--   "cordero ballena Alaxander"     -> CORDERO BALLENA ALEXANDER KLIMOVICH (worker 12211)
--   "PALMA JIMENEZ"                 -> PALMA JIMENEZ MARIA MAGALY          (worker 12349)
UPDATE ssoma_inspeccion SET inspector_worker_id = 13330 WHERE id IN (5497, 5501, 5502) AND inspector_worker_id IS NULL;
UPDATE ssoma_inspeccion SET inspector_worker_id = 12211 WHERE id = 23   AND inspector_worker_id IS NULL;
UPDATE ssoma_inspeccion SET inspector_worker_id = 12349 WHERE id = 5509 AND inspector_worker_id IS NULL;

-- ── 5. ssoma_opt: no necesita columna nueva (observador_id ya existe y está al 97%),
--       pero se completa el puñado de registros viejos que quedaron sin él ─────────
CREATE TEMP TABLE tmp_opt_canon ON COMMIT DROP AS
SELECT id,
       array(SELECT DISTINCT tk FROM unnest(
           array_remove(string_to_array(
               regexp_replace(
                   upper(translate(observador_nombre,
                                   'ÁÉÍÓÚÀÈÌÒÙÄËÏÖÜÑáéíóúàèìòùñ',
                                   'AEIOUAEIOUAEIOUNaeiouaeioun')),
                   '[^A-Za-z0-9 ]', ' ', 'g'), ' '), '')) tk ORDER BY tk) AS tokens
FROM   ssoma_opt
WHERE  observador_id IS NULL
  AND  observador_nombre IS NOT NULL;

WITH sup_unica AS (
    SELECT tokens, min(worker_id) AS worker_id
    FROM   tmp_sup_canon
    WHERE  array_length(tokens, 1) >= 1
    GROUP BY tokens
    HAVING count(DISTINCT worker_id) = 1
)
UPDATE ssoma_opt o
SET    observador_id = s.worker_id
FROM   tmp_opt_canon c
JOIN   sup_unica s ON s.tokens = c.tokens
WHERE  o.id = c.id
  AND  o.observador_id IS NULL;

-- Único OPT que la normalización no resuelve (solo apellidos, sin nombres)
UPDATE ssoma_opt SET observador_id = 12349 WHERE id = 6359 AND observador_id IS NULL;

COMMIT;

-- ── Verificación (correr después) ───────────────────────────────────────────────
-- SELECT 'inspeccion' AS tabla, count(*) AS total, count(inspector_worker_id) AS con_worker,
--        round(100.0 * count(inspector_worker_id) / nullif(count(*),0), 1) AS pct
-- FROM   ssoma_inspeccion WHERE fecha >= '2026-06-01'
-- UNION ALL
-- SELECT 'opt', count(*), count(observador_id),
--        round(100.0 * count(observador_id) / nullif(count(*),0), 1)
-- FROM   ssoma_opt WHERE fecha >= '2026-06-01';
