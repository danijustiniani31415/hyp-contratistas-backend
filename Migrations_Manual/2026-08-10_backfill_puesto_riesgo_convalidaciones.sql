-- Backfill retroactivo: completa puesto_origen/destino, clasificación y cambio_riesgo en
-- convalidaciones creadas ANTES de hoy (cuando esas columnas no existían o no se llenaban).
-- Misma lógica que ConvalidacionRepository.ResolverPuestoOrigenAsync/ResolverPuestoDestinoAsync:
--   Origen  = vinculación del trabajador en la empresa de origen del EMO, vigente a la fecha del EMO.
--   Destino = vinculación vigente actual del trabajador.
--   Si no hay vinculación que calce, se usa el dato actual del trabajador (workers.ocupacion /
--   workers.obra_oficina_staff_id) como aproximación, igual que hace el backend.
-- Ejecutar manualmente en pgAdmin, paso por paso.

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 1 — Vista previa: cuántas convalidaciones les falta algún dato.
-- ═══════════════════════════════════════════════════════════════════════════════
SELECT
  count(*) FILTER (WHERE cv.puesto_origen IS NULL OR cv.obra_oficina_staff_origen_id IS NULL) AS sin_origen,
  count(*) FILTER (WHERE cv.puesto_destino IS NULL OR cv.obra_oficina_staff_destino_id IS NULL) AS sin_destino,
  count(*) AS total
FROM worker_emo_convalidaciones cv;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 2 — Backfill, dentro de una transacción.
-- ═══════════════════════════════════════════════════════════════════════════════
BEGIN;

-- 2a) Origen: vinculación en la empresa de origen del EMO, vigente a la fecha del EMO.
UPDATE worker_emo_convalidaciones cv
SET puesto_origen = vo.puesto,
    obra_oficina_staff_origen_id = vo.obra_oficina_staff_id
FROM worker_emos e
LEFT JOIN LATERAL (
  SELECT v.puesto, v.obra_oficina_staff_id
  FROM worker_vinculaciones v
  WHERE v.worker_id = e.worker_id
    AND v.empresa_id = e.empresa_origen_id
    AND v.fecha_inicio <= e.fecha_emo
  ORDER BY v.fecha_inicio DESC
  LIMIT 1
) vo ON true
WHERE cv.emo_id = e.id
  AND (cv.puesto_origen IS NULL OR cv.obra_oficina_staff_origen_id IS NULL)
  AND (vo.puesto IS NOT NULL OR vo.obra_oficina_staff_id IS NOT NULL);

-- 2b) Origen (fallback): si no hubo vinculación que calce, usar el dato actual del trabajador.
UPDATE worker_emo_convalidaciones cv
SET puesto_origen = COALESCE(cv.puesto_origen, w.ocupacion),
    obra_oficina_staff_origen_id = COALESCE(cv.obra_oficina_staff_origen_id, w.obra_oficina_staff_id)
FROM worker_emos e
JOIN workers w ON w.id = e.worker_id
WHERE cv.emo_id = e.id
  AND (cv.puesto_origen IS NULL OR cv.obra_oficina_staff_origen_id IS NULL);

-- 2c) Destino: vinculación vigente actual del trabajador.
UPDATE worker_emo_convalidaciones cv
SET puesto_destino = vd.puesto,
    obra_oficina_staff_destino_id = vd.obra_oficina_staff_id
FROM worker_emos e
LEFT JOIN LATERAL (
  SELECT v.puesto, v.obra_oficina_staff_id
  FROM worker_vinculaciones v
  WHERE v.worker_id = e.worker_id AND v.fecha_fin IS NULL
  ORDER BY v.fecha_inicio DESC
  LIMIT 1
) vd ON true
WHERE cv.emo_id = e.id
  AND (cv.puesto_destino IS NULL OR cv.obra_oficina_staff_destino_id IS NULL)
  AND (vd.puesto IS NOT NULL OR vd.obra_oficina_staff_id IS NOT NULL);

-- 2d) Destino (fallback): igual que 2b, con el dato actual del trabajador.
UPDATE worker_emo_convalidaciones cv
SET puesto_destino = COALESCE(cv.puesto_destino, w.ocupacion),
    obra_oficina_staff_destino_id = COALESCE(cv.obra_oficina_staff_destino_id, w.obra_oficina_staff_id)
FROM worker_emos e
JOIN workers w ON w.id = e.worker_id
WHERE cv.emo_id = e.id
  AND (cv.puesto_destino IS NULL OR cv.obra_oficina_staff_destino_id IS NULL);

-- 2e) Recalcular cambio_riesgo: Oficina Central (3) → Staff/Obra (2/1) es el único caso crítico.
UPDATE worker_emo_convalidaciones cv
SET cambio_riesgo = (cv.obra_oficina_staff_origen_id = 3 AND cv.obra_oficina_staff_destino_id IN (1, 2))
WHERE cv.cambio_riesgo IS DISTINCT FROM
      (cv.obra_oficina_staff_origen_id = 3 AND cv.obra_oficina_staff_destino_id IN (1, 2));

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 3 — Verifica que ya no queden convalidaciones sin datos (debería dar 0 en ambas).
-- ═══════════════════════════════════════════════════════════════════════════════
SELECT
  count(*) FILTER (WHERE puesto_origen IS NULL OR obra_oficina_staff_origen_id IS NULL) AS sin_origen,
  count(*) FILTER (WHERE puesto_destino IS NULL OR obra_oficina_staff_destino_id IS NULL) AS sin_destino
FROM worker_emo_convalidaciones;

-- Si los números del PASO 3 se ven bien (0, o cercanos a 0 si hay EMOs/trabajadores sin
-- ninguna vinculación registrada, lo cual es un problema de datos aparte, no de este script):
--   COMMIT;
-- Si algo no cuadra:
--   ROLLBACK;
