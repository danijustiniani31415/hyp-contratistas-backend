-- Por qué solo 34 empresas si se esperan ~140 contratistas.

-- 1) Total de empresas contratistas registradas en el sistema (universo real).
SELECT count(*) AS total_contributor FROM contributor;

-- 2) De esas, ¿cuántas tienen AL MENOS UN worker activo tipo Contratista (sin filtrar por puesto)?
SELECT count(DISTINCT w.contributor_id) AS empresas_con_algun_worker_activo
FROM workers w
WHERE w.contrata_casa = 'Contratista' AND w.workers_estado_id = 1;

-- 3) De esas, ¿cuántas tienen algún worker con vinculación vigente a un proyecto
--    (worker_vinculaciones, fecha_fin IS NULL), sin filtrar por puesto?
SELECT count(DISTINCT w.contributor_id) AS empresas_con_worker_vinculado_a_proyecto
FROM workers w
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista' AND w.workers_estado_id = 1;

-- 4) De esas, ¿cuántas tienen puesto_id poblado (no NULL) en alguno de sus workers?
SELECT count(DISTINCT w.contributor_id) AS empresas_con_puesto_id_poblado
FROM workers w
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista' AND w.workers_estado_id = 1
  AND w.puesto_id IS NOT NULL;

-- 5) Los 10 puestos MÁS comunes entre workers Contratista con vinculación vigente
--    (para ver qué texto usan realmente las empresas que no calzaron con la lista).
SELECT COALESCE(pu.nombre, '(sin puesto_id)') AS puesto, count(*) AS total
FROM workers w
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
WHERE w.contrata_casa = 'Contratista' AND w.workers_estado_id = 1
GROUP BY pu.nombre
ORDER BY total DESC
LIMIT 20;

-- 6) De las empresas que SÍ tienen worker vinculado a proyecto pero NO aparecen en el
--    resultado de 83, ¿qué puesto tiene su gente? (para saber si falta agregar puestos
--    a la lista, o si de verdad no tienen a nadie con rol de supervisión).
SELECT c.contributor_name, COALESCE(pu.nombre, '(sin puesto_id)') AS puesto, count(*) AS total
FROM workers w
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN contributor c ON c.contributor_id = w.contributor_id
WHERE w.contrata_casa = 'Contratista' AND w.workers_estado_id = 1
  AND w.contributor_id NOT IN (
    SELECT w2.contributor_id
    FROM workers w2
    JOIN puesto pu2 ON pu2.puesto_id = w2.puesto_id
    JOIN worker_vinculaciones wv2 ON wv2.worker_id = w2.id AND wv2.fecha_fin IS NULL
    WHERE w2.contrata_casa = 'Contratista' AND w2.workers_estado_id = 1
      AND upper(pu2.nombre) IN (
        'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
        'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
        'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
      )
  )
GROUP BY c.contributor_name, pu.nombre
ORDER BY c.contributor_name, total DESC
LIMIT 100;
