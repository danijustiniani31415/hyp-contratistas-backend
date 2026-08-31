-- Migración manual (pgAdmin) — habilita la nueva pestaña "Observaciones" para
-- todos los roles que ya ven "arquitectura-comercial.dashboard" (acceso abierto
-- por ahora; los roles específicos se ajustan después vía la pantalla de
-- Configuración > Roles y Permisos).

INSERT INTO feature (feature_key, module_id)
SELECT 'arquitectura-comercial.observaciones', module_id
FROM feature
WHERE feature_key = 'arquitectura-comercial.dashboard'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'arquitectura-comercial.observaciones');

INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, f_new.feature_id
FROM role_feature rf
JOIN feature f_old ON f_old.feature_id = rf.feature_id AND f_old.feature_key = 'arquitectura-comercial.dashboard'
JOIN feature f_new ON f_new.feature_key = 'arquitectura-comercial.observaciones'
WHERE NOT EXISTS (
    SELECT 1 FROM role_feature rf2 WHERE rf2.role_id = rf.role_id AND rf2.feature_id = f_new.feature_id
);
