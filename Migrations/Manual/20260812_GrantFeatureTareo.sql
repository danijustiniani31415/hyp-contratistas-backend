-- Migración manual (pgAdmin) — habilita las pestañas del nuevo módulo "Tareo" para
-- todos los roles que ya ven "arquitectura-comercial.dashboard" (acceso abierto por
-- ahora; se ajusta después vía Configuración > Roles y Permisos, igual que se hizo
-- con Observaciones en 20260713_GrantFeatureObservaciones.sql).

DO $$
DECLARE
    v_key TEXT;
BEGIN
    FOREACH v_key IN ARRAY ARRAY[
        'arquitectura-comercial.tareo.marcar',
        'arquitectura-comercial.tareo.enrolamiento',
        'arquitectura-comercial.tareo.revision',
        'arquitectura-comercial.tareo.reporte'
    ]
    LOOP
        INSERT INTO feature (feature_key, module_id)
        SELECT v_key, module_id
        FROM feature
        WHERE feature_key = 'arquitectura-comercial.dashboard'
          AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = v_key);

        INSERT INTO role_feature (role_id, feature_id)
        SELECT rf.role_id, f_new.feature_id
        FROM role_feature rf
        JOIN feature f_old ON f_old.feature_id = rf.feature_id AND f_old.feature_key = 'arquitectura-comercial.dashboard'
        JOIN feature f_new ON f_new.feature_key = v_key
        WHERE NOT EXISTS (
            SELECT 1 FROM role_feature rf2 WHERE rf2.role_id = rf.role_id AND rf2.feature_id = f_new.feature_id
        );
    END LOOP;
END $$;
