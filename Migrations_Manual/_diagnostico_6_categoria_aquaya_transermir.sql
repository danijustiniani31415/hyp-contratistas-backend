SELECT w.id, w.apellido_nombre, w.contributor_id, w.contrata_casa, w.workers_estado_id,
       w.puesto_id, pu.nombre AS puesto_nombre,
       w.categoria_id, cat.nombre AS categoria_nombre
FROM workers w
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN categoria cat ON cat.categoria_id = w.categoria_id
WHERE w.contributor_id IN (648, 655)
ORDER BY w.contributor_id, w.id;
