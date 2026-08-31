-- Nuevo feature para administrar los catálogos de Tipos de Equipo e Ítems de Equipo
-- (antes cualquier usuario autenticado podía pegarle a los endpoints de alta/edición/toggle
-- de tipos-equipo sin ningún RequireFeature — se cierra ese hueco de paso). Se asigna a
-- Jefe SSOMA (9), Coordinador SSOMA (70) y Prevencionista (72) — mismos ids estables que
-- Shared/Constants/Roles.cs. Idempotente. Ejecutar manualmente en pgAdmin.

BEGIN;

INSERT INTO feature (feature_key, module_id)
SELECT 'habilitacion.catalogos.equipos', m.module_id
FROM module m
WHERE m.module_name = 'SSOMA'
  AND NOT EXISTS (
      SELECT 1 FROM feature WHERE feature_key = 'habilitacion.catalogos.equipos'
  );

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE f.feature_key = 'habilitacion.catalogos.equipos'
  AND r.role_id IN (9, 70, 72) -- Jefe SSOMA, Coordinador SSOMA, Prevencionista
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );

COMMIT;
