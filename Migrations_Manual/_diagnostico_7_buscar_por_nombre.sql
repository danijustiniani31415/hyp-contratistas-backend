-- El filtro por contributor_id 648/655 dio vacío — busquemos a estas personas por
-- nombre para ver qué contributor_id tienen realmente sus workers.

SELECT w.id, w.apellido_nombre, w.contributor_id, c.contributor_name,
       w.contrata_casa, w.workers_estado_id, w.puesto_id, pu.nombre AS puesto_nombre,
       w.categoria_id, cat.nombre AS categoria_nombre
FROM workers w
LEFT JOIN contributor c ON c.contributor_id = w.contributor_id
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN categoria cat ON cat.categoria_id = w.categoria_id
WHERE upper(w.apellido_nombre) LIKE '%JANAMPA%'
   OR upper(w.apellido_nombre) LIKE '%MONTERO%ANTUANETH%'
   OR upper(w.apellido_nombre) LIKE '%LEANDRO%MONTERO%';

-- Y de paso, cómo se llama realmente TRANSERMIR/AQUAYA en la tabla contributor
-- (por si el id que saqué antes no correspondía).
SELECT contributor_id, contributor_name FROM contributor
WHERE upper(contributor_name) LIKE '%TRANSERMIR%' OR upper(contributor_name) LIKE '%AQUAYA%';
