-- Con el criterio final (palabra clave + empresa por vinculación), estas son las
-- empresas contratistas que TODAVÍA no tienen a nadie evaluable en este flujo —
-- ya sea porque no tienen personal cargado, o porque su personal cargado no tiene
-- un puesto que contenga SUPERVISOR/CAPATAZ/PREVENCIONISTA/INGENIERO DE PRODUCCION.

SELECT c.contributor_id, c.contributor_name
FROM contributor c
WHERE NOT EXISTS (
    SELECT 1
    FROM workers w
    JOIN puesto pu ON pu.puesto_id = w.puesto_id
    JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
    WHERE COALESCE(wv.empresa_id, w.contributor_id) = c.contributor_id
      AND w.contrata_casa = 'Contratista'
      AND w.workers_estado_id = 1
      AND upper(pu.nombre) LIKE ANY(ARRAY['%SUPERVISOR%','%CAPATAZ%','%PREVENCIONISTA%','%INGENIERO DE PRODUCCION%'])
)
ORDER BY c.contributor_name;
