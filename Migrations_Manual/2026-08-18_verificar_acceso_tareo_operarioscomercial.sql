-- Verificación + otorgamiento de acceso a Arquitectura Comercial > Tareo > Marcar
-- para el usuario operarioscomercial@abril.pe. Ejecutar en pgAdmin paso a paso.

-- 1) Verificar que el usuario existe y ver su(s) rol(es) actuales
SELECT u.user_id, u.email, u.active, u.state,
       r.role_id, r.role_description
FROM app_user u
LEFT JOIN user_role ur ON ur.user_id = u.user_id AND ur.active = true AND ur.state = true
LEFT JOIN role r ON r.role_id = ur.role_id
WHERE u.email = 'operarioscomercial@abril.pe';

-- 2) Ver si esos roles ya tienen acceso a arquitectura-comercial.tareo.marcar
SELECT r.role_id, r.role_description, f.feature_key
FROM role r
JOIN role_feature rf ON rf.role_id = r.role_id
JOIN feature f ON f.feature_id = rf.feature_id
WHERE f.feature_key LIKE 'arquitectura-comercial.tareo%'
  AND r.role_id IN (
    SELECT ur.role_id FROM user_role ur
    JOIN app_user u ON u.user_id = ur.user_id
    WHERE u.email = 'operarioscomercial@abril.pe' AND ur.active = true AND ur.state = true
  );

-- 3) Si el paso 2 no devuelve 'arquitectura-comercial.tareo.marcar' para el rol del
--    usuario, otorgar el feature a ese/esos rol(es) (mismo patrón que
--    20260812_GrantFeatureTareo.sql, pero dirigido solo a los roles de este usuario).
INSERT INTO role_feature (role_id, feature_id)
SELECT DISTINCT ur.role_id, f.feature_id
FROM user_role ur
JOIN app_user u ON u.user_id = ur.user_id
JOIN feature f ON f.feature_key = 'arquitectura-comercial.tareo.marcar'
WHERE u.email = 'operarioscomercial@abril.pe'
  AND ur.active = true AND ur.state = true
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf2
    WHERE rf2.role_id = ur.role_id AND rf2.feature_id = f.feature_id
  );

-- 4) Confirmar el resultado
SELECT r.role_id, r.role_description, f.feature_key
FROM role r
JOIN role_feature rf ON rf.role_id = r.role_id
JOIN feature f ON f.feature_id = rf.feature_id
WHERE f.feature_key = 'arquitectura-comercial.tareo.marcar'
  AND r.role_id IN (
    SELECT ur.role_id FROM user_role ur
    JOIN app_user u ON u.user_id = ur.user_id
    WHERE u.email = 'operarioscomercial@abril.pe' AND ur.active = true AND ur.state = true
  );
