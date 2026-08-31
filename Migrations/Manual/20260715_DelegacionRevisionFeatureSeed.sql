-- ============================================================================
-- Seed de la funcionalidad principal "Delegación de Revisión"
-- Fecha: 2026-07-15
--
-- 1) feature_key nuevo (module_id 10 = Gestión Administrativa).
-- 2) Acceso para el rol 76 (ADMINISTRADOR DE SOLICITUD DE SALIDAS). Se usa subselect
--    por feature_key porque el feature_id difiere entre dev y prod.
-- 3) Asignar rol 76 a TODOS los revisores vivos (workers_revisores / area_revisores)
--    que tengan cuenta y aún no lo tengan, para que puedan entrar a la funcionalidad.
--    OJO: el rol 76 es el "admin del módulo": darlo a un revisor que no sea J/C/G
--    también le abre Configuración de Gestión Administrativa.
-- ============================================================================

-- 1) Feature ------------------------------------------------------------------
INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-administrativa.delegacion-revision', 10
WHERE NOT EXISTS (
    SELECT 1 FROM feature WHERE feature_key = 'gestion-administrativa.delegacion-revision'
);

-- 2) role_feature para rol 76 -------------------------------------------------
INSERT INTO role_feature (role_id, feature_id)
SELECT 76, f.feature_id
FROM feature f
WHERE f.feature_key = 'gestion-administrativa.delegacion-revision'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = 76 AND rf.feature_id = f.feature_id
  );

-- 3) Asignar rol 76 a todos los revisores vivos sin el rol -------------------
INSERT INTO user_role (user_id, role_id, created_date_time, created_user_id, active, state)
SELECT DISTINCT p.user_id, 76, (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'), 1, true, true
FROM (
    SELECT revisor_id AS worker_id FROM workers_revisores WHERE state
    UNION
    SELECT revisor_id AS worker_id FROM area_revisores WHERE state
) r
JOIN workers w ON w.id = r.worker_id
JOIN person  p ON p.person_id = w.person_id
WHERE p.user_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM user_role ur
      WHERE ur.user_id = p.user_id AND ur.role_id = 76 AND ur.state = true
  );
