-- El conteo dio 0 — vamos a encontrar en cuál filtro se pierden todos.

-- 1) ¿Existen esos puestos tal cual, sin importar nada más?
SELECT puesto_id, nombre FROM puesto
WHERE upper(nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
);

-- 2) ¿Cuántos workers tienen esos puesto_id, sin filtrar por nada más?
SELECT pu.nombre, count(*) AS total_workers
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
WHERE upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
)
GROUP BY pu.nombre;

-- 3) De esos, ¿cuántos son contrata_casa = 'Contrata' y cuántos workers_estado_id = 1?
SELECT w.contrata_casa, w.workers_estado_id, count(*) AS total
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
WHERE upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
)
GROUP BY w.contrata_casa, w.workers_estado_id
ORDER BY total DESC;

-- 4) De los que sí son Contrata + activo, ¿cuántos tienen fila en worker_vinculaciones
--    con fecha_fin IS NULL (proyecto vigente)?
SELECT
  count(*) AS total_contrata_activos,
  count(wv.id) AS con_vinculacion_vigente
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contrata'
  AND w.workers_estado_id = 1
  AND upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
  );

-- 5) Distintos valores que toma contrata_casa en toda la tabla (por si no es 'Contrata' exacto).
SELECT contrata_casa, count(*) FROM workers GROUP BY contrata_casa;
