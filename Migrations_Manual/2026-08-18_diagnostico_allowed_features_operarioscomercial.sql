-- Diagnóstico: replica EXACTAMENTE la consulta que usa el backend
-- (AuthRepository.GetAllowedFeaturesAsync) para armar allowed_features al loguear.
-- Si esto no devuelve 'arquitectura-comercial.tareo.marcar', el sidebar no lo mostrará
-- sin importar lo que haya en role_feature.

-- 1) Estado detallado (active Y state) del user_role y del role para este usuario
SELECT u.user_id, u.email, u.active AS user_active, u.state AS user_state,
       ur.user_role_id, ur.active AS user_role_active, ur.state AS user_role_state,
       r.role_id, r.role_description, r.active AS role_active, r.state AS role_state
FROM app_user u
JOIN user_role ur ON ur.user_id = u.user_id
JOIN role r ON r.role_id = ur.role_id
WHERE u.email = 'operarioscomercial@abril.pe';

-- 2) La consulta EXACTA del backend (AuthRepository.cs) para este usuario
SELECT DISTINCT f.feature_key
FROM feature f
JOIN role_feature rf ON rf.feature_id = f.feature_id
JOIN user_role ur     ON ur.role_id   = rf.role_id
JOIN role r           ON r.role_id    = ur.role_id
WHERE ur.user_id = (SELECT user_id FROM app_user WHERE email = 'operarioscomercial@abril.pe')
  AND ur.state
  AND r.state;
