-- Agrega tracking histórico de CATEGORÍA (campo de lógica, catalogo `categoria`) en paralelo
-- al de PUESTO (campo de presentación) que ya existía:
--   1) worker_vinculaciones.categoria_id — se congela en cada cambio de obra/reingreso/alta,
--      igual que ya se hacía con `puesto` y `obra_oficina_staff_id`.
--   2) worker_emo_convalidaciones.categoria_origen / categoria_destino — nombre congelado al
--      momento de resolver la convalidación, igual que puesto_origen/puesto_destino.
--
-- También corrige el dato sucio detectado en la convalidación de REYES CARBAJAL WELINTON
-- ESTEBAN (DNI 75998430): su vinculación de origen quedó con puesto = "ALBAÑIL" cuando el
-- puesto real (origen y destino) era "VOLANTE" — solo cambió la empresa (convalidación), no
-- el puesto. Ese texto es de tipeo libre (worker_vinculaciones.puesto), no derivado de ningún
-- catálogo, así que no hay forma de detectarlo automáticamente; se corrige a mano.
--
-- Ejecutar manualmente en pgAdmin, PASO por PASO.

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 1 — Columnas nuevas.
-- ═══════════════════════════════════════════════════════════════════════════════
BEGIN;

ALTER TABLE worker_vinculaciones
  ADD COLUMN IF NOT EXISTS categoria_id integer NULL
    REFERENCES categoria (categoria_id);

ALTER TABLE worker_emo_convalidaciones
  ADD COLUMN IF NOT EXISTS categoria_origen  varchar NULL,
  ADD COLUMN IF NOT EXISTS categoria_destino varchar NULL;

COMMIT;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 2 — Backfill de worker_vinculaciones.categoria_id (mejor esfuerzo: no hay categoría
-- histórica guardada en ningún lado antes de hoy, así que se usa la categoría ACTUAL del
-- trabajador para TODAS sus vinculaciones — igual aproximación que ya se acepta para
-- convalidaciones antiguas sin puesto_origen/destino).
-- ═══════════════════════════════════════════════════════════════════════════════
BEGIN;

UPDATE worker_vinculaciones v
SET categoria_id = w.categoria_id
FROM workers w
WHERE w.id = v.worker_id
  AND v.categoria_id IS NULL
  AND w.categoria_id IS NOT NULL;

COMMIT;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 3 — Backfill de worker_emo_convalidaciones.categoria_origen/destino, misma lógica que
-- ConvalidacionRepository.ResolverCategoriaOrigenAsync/ResolverCategoriaDestinoAsync.
-- ═══════════════════════════════════════════════════════════════════════════════
BEGIN;

-- 3a) Origen: vinculación en la empresa de origen del EMO, vigente a la fecha del EMO.
UPDATE worker_emo_convalidaciones cv
SET categoria_origen = co.nombre
FROM worker_emos e
LEFT JOIN LATERAL (
  SELECT c.nombre
  FROM worker_vinculaciones v
  JOIN categoria c ON c.categoria_id = v.categoria_id
  WHERE v.worker_id = e.worker_id
    AND v.empresa_id = e.empresa_origen_id
    AND v.fecha_inicio <= e.fecha_emo
  ORDER BY v.fecha_inicio DESC
  LIMIT 1
) co ON true
WHERE cv.emo_id = e.id
  AND cv.categoria_origen IS NULL
  AND co.nombre IS NOT NULL;

-- 3b) Origen (fallback): sin vinculación que calce, usar la categoría actual del trabajador.
UPDATE worker_emo_convalidaciones cv
SET categoria_origen = cw.nombre
FROM worker_emos e
JOIN workers w ON w.id = e.worker_id
LEFT JOIN categoria cw ON cw.categoria_id = w.categoria_id
WHERE cv.emo_id = e.id
  AND cv.categoria_origen IS NULL
  AND cw.nombre IS NOT NULL;

-- 3c) Destino: vinculación vigente actual del trabajador.
UPDATE worker_emo_convalidaciones cv
SET categoria_destino = cd.nombre
FROM worker_emos e
LEFT JOIN LATERAL (
  SELECT c.nombre
  FROM worker_vinculaciones v
  JOIN categoria c ON c.categoria_id = v.categoria_id
  WHERE v.worker_id = e.worker_id AND v.fecha_fin IS NULL
  ORDER BY v.fecha_inicio DESC
  LIMIT 1
) cd ON true
WHERE cv.emo_id = e.id
  AND cv.categoria_destino IS NULL
  AND cd.nombre IS NOT NULL;

-- 3d) Destino (fallback): igual que 3b.
UPDATE worker_emo_convalidaciones cv
SET categoria_destino = cw.nombre
FROM worker_emos e
JOIN workers w ON w.id = e.worker_id
LEFT JOIN categoria cw ON cw.categoria_id = w.categoria_id
WHERE cv.emo_id = e.id
  AND cv.categoria_destino IS NULL
  AND cw.nombre IS NOT NULL;

-- Verifica antes de cerrar la transacción (debería dar 0, o cercano a 0 si hay EMOs de
-- trabajadores sin categoría asignada en absoluto — problema de datos aparte):
SELECT
  count(*) FILTER (WHERE categoria_origen IS NULL)  AS sin_origen,
  count(*) FILTER (WHERE categoria_destino IS NULL) AS sin_destino
FROM worker_emo_convalidaciones;

-- Si se ve bien:
--   COMMIT;
-- Si algo no cuadra:
--   ROLLBACK;

-- ═══════════════════════════════════════════════════════════════════════════════
-- PASO 4 — Fix puntual: REYES CARBAJAL WELINTON ESTEBAN (DNI 75998430).
-- No hay convalidación "Pendiente" activa (se descartó/recreó varias veces en pruebas) — las
-- 4 quedaron con puesto_origen/destino en ping-pong ALBAÑIL↔VOLANTE. La verdad real: el
-- trabajador SIEMPRE fue VOLANTE, nunca hubo cambio de puesto (solo cambió la empresa/razón
-- social). Se corrigen TODAS sus convalidaciones (no solo la Pendiente, que no existe) a
-- VOLANTE-VOLANTE, sin filtrar por resultado.
-- ═══════════════════════════════════════════════════════════════════════════════

-- 4a) Vista previa — debería mostrar las filas con id 64-67 (y cualquier otra de este
-- trabajador) con puesto_origen/destino distintos de VOLANTE.
SELECT cv.id, cv.puesto_origen, cv.puesto_destino, cv.categoria_origen, cv.categoria_destino,
       cv.resultado, w.apellido_nombre
FROM worker_emo_convalidaciones cv
JOIN worker_emos e ON e.id = cv.emo_id
JOIN workers w ON w.id = e.worker_id
JOIN person p ON p.person_id = w.person_id
WHERE p.document_identity_code = '75998430'
ORDER BY cv.id DESC;

-- 4b) Corrección: todas las convalidaciones de este trabajador quedan VOLANTE-VOLANTE.
BEGIN;

UPDATE worker_emo_convalidaciones cv
SET puesto_origen = 'VOLANTE',
    puesto_destino = 'VOLANTE'
FROM worker_emos e
JOIN workers w ON w.id = e.worker_id
JOIN person p ON p.person_id = w.person_id
WHERE cv.emo_id = e.id
  AND p.document_identity_code = '75998430';

-- También corrige el dato de origen en la(s) vinculación(es) (para que futuras
-- convalidaciones de este trabajador no vuelvan a arrastrar el texto incorrecto).
UPDATE worker_vinculaciones v
SET puesto = 'VOLANTE'
FROM workers w
JOIN person p ON p.person_id = w.person_id
WHERE v.worker_id = w.id
  AND p.document_identity_code = '75998430'
  AND v.puesto = 'ALBAÑIL';

-- Verifica:
SELECT cv.id, cv.puesto_origen, cv.puesto_destino
FROM worker_emo_convalidaciones cv
JOIN worker_emos e ON e.id = cv.emo_id
JOIN workers w ON w.id = e.worker_id
JOIN person p ON p.person_id = w.person_id
WHERE p.document_identity_code = '75998430'
ORDER BY cv.id DESC;

-- Si todas las filas quedaron con puesto_origen = puesto_destino = 'VOLANTE':
--   COMMIT;
-- Si algo no cuadra:
--   ROLLBACK;
