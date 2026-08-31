-- Backfill de observador_id en ssoma_opt a partir del nombre guardado (observador_nombre),
-- para las OPT creadas antes de este cambio (donde solo se guardaba el nombre en texto libre).
-- Solo actualiza cuando el nombre matchea EXACTAMENTE UN trabajador (sin ambigüedad).
-- Si matchea 0 o 2+ trabajadores, la fila queda igual (revisar manualmente con el reporte de abajo).

-- ── 1. REPORTE (solo lectura) — revisa esto antes de correr el UPDATE ──────────────
-- Muestra cada OPT sin observador_id, el nombre guardado, y cuántos trabajadores
-- coinciden con ese nombre (0 = no encontrado, 1 = match seguro, 2+ = ambiguo).
SELECT
  o.id AS opt_id,
  o.fecha,
  o.observador_nombre,
  COUNT(w.id) AS candidatos_encontrados,
  STRING_AGG(DISTINCT p.full_name, ' | ') AS nombres_candidatos
FROM ssoma_opt o
LEFT JOIN person p ON UPPER(TRIM(p.full_name)) = UPPER(TRIM(o.observador_nombre))
LEFT JOIN workers w ON w.person_id = p.person_id
WHERE o.observador_id IS NULL
  AND o.observador_nombre IS NOT NULL
  AND TRIM(o.observador_nombre) <> ''
GROUP BY o.id, o.fecha, o.observador_nombre
ORDER BY candidatos_encontrados DESC, o.fecha DESC;

-- ── 2. BACKFILL seguro (solo matches únicos) ───────────────────────────────────────
-- Descomenta y corre esto DESPUÉS de revisar el reporte de arriba.
/*
WITH match_unico AS (
  SELECT
    o.id AS opt_id,
    MIN(w.id) AS worker_id,
    COUNT(w.id) AS candidatos
  FROM ssoma_opt o
  JOIN person p ON UPPER(TRIM(p.full_name)) = UPPER(TRIM(o.observador_nombre))
  JOIN workers w ON w.person_id = p.person_id
  WHERE o.observador_id IS NULL
    AND o.observador_nombre IS NOT NULL
    AND TRIM(o.observador_nombre) <> ''
  GROUP BY o.id
  HAVING COUNT(w.id) = 1
)
UPDATE ssoma_opt o
SET observador_id = m.worker_id
FROM match_unico m
WHERE o.id = m.opt_id;
*/
