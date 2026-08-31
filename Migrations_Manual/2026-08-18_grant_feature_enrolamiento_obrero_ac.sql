-- Migración manual (pgAdmin) — otorga 'arquitectura-comercial.tareo.enrolamiento' al rol
-- OBRERO-AC (role_id 75), usado por la cuenta corporativa compartida operarioscomercial@abril.pe.
-- Habilita SOLO la pantalla de enrolamiento asistido (elegir su nombre + tomarse la foto, una
-- vez que el coordinador subió su SSO-FO-150). NO otorga 'arquitectura-comercial.tareo.gestion-permisos'
-- (subir/descargar autorizaciones, geolocalización) — eso sigue exclusivo del rol Gestor AC (51),
-- ver 20260814_GrantFeatureGestionPermisos.sql.

INSERT INTO role_feature (role_id, feature_id)
SELECT 75, f.feature_id
FROM feature f
WHERE f.feature_key = 'arquitectura-comercial.tareo.enrolamiento'
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf WHERE rf.role_id = 75 AND rf.feature_id = f.feature_id
  );

-- Verificación
SELECT r.role_id, r.role_description, f.feature_key
FROM role r
JOIN role_feature rf ON rf.role_id = r.role_id
JOIN feature f ON f.feature_id = rf.feature_id
WHERE r.role_id = 75 AND f.feature_key LIKE 'arquitectura-comercial.tareo%';
