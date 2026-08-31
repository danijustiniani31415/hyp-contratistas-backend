SELECT w.id, w.apellido_nombre, per.full_name, w.contributor_id, c.contributor_name,
       w.contrata_casa, w.workers_estado_id, w.puesto_id, pu.nombre AS puesto_nombre,
       w.categoria_id, cat.nombre AS categoria_nombre
FROM workers w
LEFT JOIN person per ON per.person_id = w.person_id
LEFT JOIN contributor c ON c.contributor_id = w.contributor_id
LEFT JOIN puesto pu ON pu.puesto_id = w.puesto_id
LEFT JOIN categoria cat ON cat.categoria_id = w.categoria_id
WHERE upper(COALESCE(w.apellido_nombre,'') || ' ' || COALESCE(per.full_name,'')) LIKE '%JANAMPA%'
   OR upper(COALESCE(w.apellido_nombre,'') || ' ' || COALESCE(per.full_name,'')) LIKE '%LEANDRO%MONTERO%'
   OR upper(COALESCE(w.apellido_nombre,'') || ' ' || COALESCE(per.full_name,'')) LIKE '%MONTERO%ANTUANETH%';
