-- AQUAYA (648) y TRANSERMIR (655) supuestamente sí tienen supervisores, pero salieron
-- en la lista de "sin personal cargado". Ver exactamente qué tienen sus workers.

SELECT w.id, w.apellido_nombre, w.contributor_id, w.contrata_casa, w.workers_estado_id,
       we.name AS estado_nombre, w.puesto_id, pu.nombre AS puesto_nombre,
       w.categoria_id, cat.nombre AS categoria_nombre
FROM workers w
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN categoria cat ON cat.categoria_id = w.categoria_id
LEFT JOIN workers_estado we ON we.id = w.workers_estado_id
WHERE w.contributor_id IN (648, 655)
ORDER BY w.contributor_id, w.id;

-- Por si el nombre de la tabla/columna de estado es distinto, alternativa sin el join:
SELECT w.id, w.apellido_nombre, w.contributor_id, w.contrata_casa, w.workers_estado_id,
       w.puesto_id, w.puesto AS puesto_texto_congelado, w.ocupacion AS ocupacion_texto_congelado
FROM workers w
WHERE w.contributor_id IN (648, 655)
ORDER BY w.contributor_id, w.id;
