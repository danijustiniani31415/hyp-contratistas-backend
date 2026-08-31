SELECT pu.nombre AS puesto, count(*) AS total_trabajadores
FROM workers w
JOIN puesto pu ON pu.puesto_id = w.puesto_id
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE w.contrata_casa = 'Contratista'
  AND w.workers_estado_id = 1
GROUP BY pu.nombre
ORDER BY pu.nombre;
