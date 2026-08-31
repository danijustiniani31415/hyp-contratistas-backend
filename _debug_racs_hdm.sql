-- 1) Todos los RAC de CORPORACION HDM SAC, en CUALQUIER proyecto, sin filtro de mes
--    (para ver si hay RACs de julio o de otros proyectos que no aparecen en la UI filtrada)
SELECT r.id, r.codigo, r.proyecto_id, pr.project_description AS proyecto_nombre,
       r.empresa_reportada_id, cr.contributor_name AS empresa_reportada,
       r.empresa_reportante_id, ce.contributor_name AS empresa_reportante,
       r.tipo, r.severidad, r.estado, r.fecha_reporte, r.created_at
FROM ssoma_rac r
LEFT JOIN project pr ON pr.project_id = r.proyecto_id
LEFT JOIN contributor cr ON cr.contributor_id = r.empresa_reportada_id
LEFT JOIN contributor ce ON ce.contributor_id = r.empresa_reportante_id
WHERE r.empresa_reportada_id = (SELECT contributor_id FROM contributor WHERE contributor_name ILIKE '%CORPORACION HDM%' LIMIT 1)
   OR r.empresa_reportante_id = (SELECT contributor_id FROM contributor WHERE contributor_name ILIKE '%CORPORACION HDM%' LIMIT 1)
ORDER BY r.created_at DESC;

-- 2) Por si el RAC quedó con empresa_reportada_id NULL o distinta (creado por error)
--    pero el reportante SÍ es HDM: ya cubierto arriba por el OR.

-- 3) Ver todos los RAC creados recientemente (últimos 7 días) en el proyecto KAURÍ,
--    sin importar la empresa, para detectar si algo se guardó con empresa equivocada
SELECT r.id, r.codigo, r.proyecto_id, r.empresa_reportada_id, cr.contributor_name AS empresa_reportada,
       r.empresa_reportante_id, ce.contributor_name AS empresa_reportante,
       r.estado, r.fecha_reporte, r.created_at
FROM ssoma_rac r
LEFT JOIN contributor cr ON cr.contributor_id = r.empresa_reportada_id
LEFT JOIN contributor ce ON ce.contributor_id = r.empresa_reportante_id
WHERE r.proyecto_id = (SELECT project_id FROM project WHERE project_description ILIKE '%KAUR%' LIMIT 1)
  AND r.created_at >= now() - interval '7 days'
ORDER BY r.created_at DESC;
