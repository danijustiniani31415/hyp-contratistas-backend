-- Migración manual (pgAdmin) — crea la feature "arquitectura-comercial.tareo.gestion-permisos"
-- (pantalla del coordinador para enrolar el rostro de cada trabajador) y la otorga SOLO al rol
-- Gestor de Arquitectura Comercial (role_id = 51, ver Shared/Constants/Roles.cs). A diferencia de
-- 20260812_GrantFeatureTareo.sql, esta feature NO se abre a todos los roles que ven el dashboard:
-- es una pantalla de administración de datos biométricos de terceros, exclusiva del coordinador.

DO $$
DECLARE
    v_module_id INTEGER;
BEGIN
    SELECT module_id INTO v_module_id
    FROM feature
    WHERE feature_key = 'arquitectura-comercial.dashboard';

    INSERT INTO feature (feature_key, module_id)
    SELECT 'arquitectura-comercial.tareo.gestion-permisos', v_module_id
    WHERE NOT EXISTS (
        SELECT 1 FROM feature WHERE feature_key = 'arquitectura-comercial.tareo.gestion-permisos'
    );

    INSERT INTO role_feature (role_id, feature_id)
    SELECT 51, f.feature_id
    FROM feature f
    WHERE f.feature_key = 'arquitectura-comercial.tareo.gestion-permisos'
      AND NOT EXISTS (
          SELECT 1 FROM role_feature rf WHERE rf.role_id = 51 AND rf.feature_id = f.feature_id
      );
END $$;
