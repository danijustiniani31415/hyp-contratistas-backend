-- Otorga el feature de la pantalla "Programación de Inducciones" (SSOMA → Gestión) a los
-- roles que ya pueden operar SSOMA: Jefe SSOMA (9), Coordinador SSOMA (70) y Prevencionista
-- (72) — mismos ids estables que Shared/Constants/Roles.cs, no nombres de texto (un rol puede
-- renombrarse desde Configuración > Roles sin que esto se rompa). Idempotente: se puede
-- correr más de una vez sin duplicar filas. Ejecutar manualmente en pgAdmin.

BEGIN;

INSERT INTO feature (feature_key, module_id)
SELECT 'ssoma.gestion.programacion-inducciones', m.module_id
FROM module m
WHERE m.module_name = 'SSOMA'
  AND NOT EXISTS (
      SELECT 1 FROM feature WHERE feature_key = 'ssoma.gestion.programacion-inducciones'
  );

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE f.feature_key = 'ssoma.gestion.programacion-inducciones'
  AND r.role_id IN (9, 70, 72) -- Jefe SSOMA, Coordinador SSOMA, Prevencionista
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );

COMMIT;
