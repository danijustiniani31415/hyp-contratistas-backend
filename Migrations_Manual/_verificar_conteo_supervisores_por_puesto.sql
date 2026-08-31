-- Cuántos supervisores va a traer ahora "Evaluar Supervisor de Contratista"
-- (misma lógica que EvSupervisorContratistaRepository.GetInicioAsync).

-- 1) Total de candidatos (trabajadores, no empresas) en cualquier proyecto.
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

-- 2) Desglose por puesto, para ver qué está aportando más.
SELECT pu.nombre AS puesto, count(*) AS total
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista'
  AND w.workers_estado_id = 1
  AND upper(pu.nombre) IN (
    'CAPATAZ','CAPATAZ SUPERVISOR DE CAMPO','SUPERVISOR','SUPERVISOR DE CAMPO',
    'PREVENCIONISTA','PREVENCIONISTA DE RIESGOS','SUPERVISOR DE ACABADOS',
    'ARQUITECTO SUPERVISOR DE CAMPO','INGENIERO DE PRODUCCION'
  )
GROUP BY pu.nombre
ORDER BY total DESC;

-- 3) Cuántas empresas (contributor) distintas y cuántos proyectos distintos aportan.
SELECT count(DISTINCT w.contributor_id) AS empresas_distintas,
       count(DISTINCT wv.proyecto_id)   AS proyectos_distintos
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
