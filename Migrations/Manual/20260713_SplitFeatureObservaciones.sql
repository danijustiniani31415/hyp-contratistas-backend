-- Migración manual (pgAdmin) — "Gestión de Observaciones" pasa a ser un
-- submódulo propio en el sidebar (antes era una pestaña dentro de
-- "arquitectura-comercial.observaciones", ahora tiene Dashboard y Lista
-- separados). Se crean los dos feature_key nuevos y se otorgan a los mismos
-- roles que ya tenían 'arquitectura-comercial.observaciones', que queda
-- deprecado (no se borra, por si algún rol quedó referenciándolo en caché).

INSERT INTO feature (feature_key, module_id)
SELECT 'arquitectura-comercial.observaciones.dashboard', module_id
FROM feature
WHERE feature_key = 'arquitectura-comercial.observaciones'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'arquitectura-comercial.observaciones.dashboard');

INSERT INTO feature (feature_key, module_id)
SELECT 'arquitectura-comercial.observaciones.lista', module_id
FROM feature
WHERE feature_key = 'arquitectura-comercial.observaciones'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'arquitectura-comercial.observaciones.lista');

INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, f_new.feature_id
FROM role_feature rf
JOIN feature f_old ON f_old.feature_id = rf.feature_id AND f_old.feature_key = 'arquitectura-comercial.observaciones'
JOIN feature f_new ON f_new.feature_key = 'arquitectura-comercial.observaciones.dashboard'
WHERE NOT EXISTS (
    SELECT 1 FROM role_feature rf2 WHERE rf2.role_id = rf.role_id AND rf2.feature_id = f_new.feature_id
);

INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, f_new.feature_id
FROM role_feature rf
JOIN feature f_old ON f_old.feature_id = rf.feature_id AND f_old.feature_key = 'arquitectura-comercial.observaciones'
JOIN feature f_new ON f_new.feature_key = 'arquitectura-comercial.observaciones.lista'
WHERE NOT EXISTS (
    SELECT 1 FROM role_feature rf2 WHERE rf2.role_id = rf.role_id AND rf2.feature_id = f_new.feature_id
);
