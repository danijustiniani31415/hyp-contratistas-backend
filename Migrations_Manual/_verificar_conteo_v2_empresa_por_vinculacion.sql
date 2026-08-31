-- Igual que la verificación anterior, pero con la empresa tomada de
-- worker_vinculaciones.empresa_id (la real) en vez de workers.contributor_id.

SELECT count(*) AS total_supervisores
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista'
  AND w.workers_estado_id = 1
  AND upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
  );

SELECT count(DISTINCT COALESCE(wv.empresa_id, w.contributor_id)) AS empresas_distintas,
       count(DISTINCT wv.proyecto_id) AS proyectos_distintos
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista'
  AND w.workers_estado_id = 1
  AND upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
  );

-- Lista de empresas que ahora sí aparecen (para chequear que TRANSERMIR/AQUAYA estén).
SELECT DISTINCT COALESCE(wv.empresa_id, w.contributor_id) AS empresa_id,
       COALESCE(c.contributor_name, 'Sin empresa') AS empresa_nombre
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
LEFT JOIN contributor c ON c.contributor_id = COALESCE(wv.empresa_id, w.contributor_id)
WHERE w.contrata_casa = 'Contratista'
  AND w.workers_estado_id = 1
  AND upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
  )
ORDER BY empresa_nombre;
