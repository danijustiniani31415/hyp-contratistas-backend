-- Migración manual (pgAdmin) — feature para editar observaciones ya reportadas
-- (el lápiz en la lista/reporte). A propósito NO se otorga a ningún rol acá:
-- el admin decide quién lo tiene desde Configuración > Roles y Permisos.

INSERT INTO feature (feature_key, module_id)
SELECT 'arquitectura-comercial.observaciones.editar', module_id
FROM feature
WHERE feature_key = 'arquitectura-comercial.observaciones'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'arquitectura-comercial.observaciones.editar');
