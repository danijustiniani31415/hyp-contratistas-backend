-- Identificar quién creó cada RAC huérfano (sin empresa reportada) para saber a qué
-- contratista corresponde antes de corregirlo manualmente.
-- NOTA: created_by es el user_id del JWT (ClaimTypes.NameIdentifier). Si el RAC lo creó
-- una cuenta de contratista (ContratistaAuthService), es posible que no exista en app_user
-- y todas las columnas "creador_*"/"empresa_vinculacion_*" salgan NULL — en ese caso hay
-- que identificar al contratista por otra vía (preguntarle directamente cuál RAC es suyo).
SELECT r.id, r.codigo, r.proyecto_id, r.created_by, au.email AS creador_email,
       p.full_name AS creador_nombre, w.id AS worker_id, w.categoria,
       wv.empresa_id AS empresa_vinculacion_actual, c.contributor_name AS empresa_vinculacion_nombre,
       r.observado_worker_id, r.es_anonimo_observado, r.empresa_reportante_id,
       r.descripcion, r.created_at
FROM ssoma_rac r
LEFT JOIN app_user au ON au.user_id = r.created_by
LEFT JOIN person p ON p.user_id = au.user_id
LEFT JOIN workers w ON w.person_id = p.person_id
LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
LEFT JOIN contributor c ON c.contributor_id = wv.empresa_id
WHERE r.codigo IN ('RAC-2026-KAU-014','RAC-2026-KAU-015','RAC-2026-KAU-017','RAC-2026-KAU-020','RAC-2026-KAU-025')
ORDER BY r.created_at;
