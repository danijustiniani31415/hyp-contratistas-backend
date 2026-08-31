-- Buscar en TODOS los campos posibles (nuevos y viejos/congelados) dónde vive
-- realmente el texto de estos puestos, porque el query por puesto.nombre dio 0.

-- 1) Catálogo nuevo unificado: puesto.nombre
SELECT 'puesto.nombre' AS fuente, nombre, count(*) OVER (PARTITION BY nombre) AS n
FROM puesto
WHERE upper(nombre) LIKE '%SUPERVIS%' OR upper(nombre) LIKE '%CAPATAZ%'
   OR upper(nombre) LIKE '%PREVENCION%' OR upper(nombre) LIKE '%PRODUCCION%'
ORDER BY nombre;

-- 2) Catálogo nuevo unificado: categoria.nombre
SELECT 'categoria.nombre' AS fuente, nombre, count(*) OVER (PARTITION BY nombre) AS n
FROM categoria
WHERE upper(nombre) LIKE '%SUPERVIS%' OR upper(nombre) LIKE '%CAPATAZ%'
   OR upper(nombre) LIKE '%PREVENCION%' OR upper(nombre) LIKE '%PRODUCCION%'
ORDER BY nombre;

-- 3) Texto congelado workers.puesto (autocompletado Categoría+Ocupación)
SELECT 'workers.puesto (texto)' AS fuente, puesto, count(*) AS n
FROM workers
WHERE upper(puesto) LIKE '%SUPERVIS%' OR upper(puesto) LIKE '%CAPATAZ%'
   OR upper(puesto) LIKE '%PREVENCION%' OR upper(puesto) LIKE '%PRODUCCION%'
GROUP BY puesto
ORDER BY n DESC;

-- 4) Texto congelado workers.ocupacion
SELECT 'workers.ocupacion (texto)' AS fuente, ocupacion, count(*) AS n
FROM workers
WHERE upper(ocupacion) LIKE '%SUPERVIS%' OR upper(ocupacion) LIKE '%CAPATAZ%'
   OR upper(ocupacion) LIKE '%PREVENCION%' OR upper(ocupacion) LIKE '%PRODUCCION%'
GROUP BY ocupacion
ORDER BY n DESC;

-- 5) Texto congelado workers.categoria
SELECT 'workers.categoria (texto)' AS fuente, categoria, count(*) AS n
FROM workers
WHERE upper(categoria) LIKE '%SUPERVIS%' OR upper(categoria) LIKE '%CAPATAZ%'
   OR upper(categoria) LIKE '%PREVENCION%' OR upper(categoria) LIKE '%PRODUCCION%'
GROUP BY categoria
ORDER BY n DESC;

-- 6) ¿Cuántos workers en total tienen puesto_id / categoria_id poblado (NULL o no)?
--    Para saber si el problema es que la migración de unificación no backfillió estos workers.
SELECT
  count(*) AS total_workers,
  count(puesto_id) AS con_puesto_id,
  count(categoria_id) AS con_categoria_id
FROM workers
WHERE contrata_casa = 'Contrata';
