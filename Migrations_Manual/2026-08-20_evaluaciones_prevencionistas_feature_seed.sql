-- ============================================================================
-- Evaluaciones · Contratistas evalúan a Prevencionista/Coordinador SSOMA (Flujo C)
-- — features + accesos
--
-- Sigue el mismo patrón que las migraciones de feature seed anteriores de
-- Evaluaciones: el module_id se reutiliza del de una feature 'evaluaciones.*'
-- ya existente ('evaluaciones.evaluar').
--
-- Nota: la pantalla de evaluar (dentro del portal contratista,
-- /habilitacion/evaluar-prevencionista) NO usa featureKey — se gatea por
-- `roles: ['CONTRATISTA']` en el frontend (mismo patrón que
-- /habilitacion/trabajadores e /habilitacion/inducciones), así que no
-- necesita fila en feature/role_feature.
--
-- Accesos:
--   evaluaciones.mi-perfil-prevencionista  → roles 70 (Coordinador SSOMA) y 72
--     (Prevencionista) — mismos roles que exige EvPrevencionistaController
--     para GET /mi-perfil.
--   evaluaciones.dashboard-prevencionistas → rol 9 (Jefe SSOMA) — mismo rol
--     que exige el backend para GET /dashboard.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Features
INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.mi-perfil-prevencionista', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.mi-perfil-prevencionista');

INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.dashboard-prevencionistas', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.dashboard-prevencionistas');

-- 2) Accesos
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (70), (72)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.mi-perfil-prevencionista'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (9)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.dashboard-prevencionistas'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT f.feature_key, array_agg(r.role_id ORDER BY r.role_id) AS roles_con_acceso
-- FROM feature f
-- JOIN role_feature rf ON rf.feature_id = f.feature_id
-- JOIN role r ON r.role_id = rf.role_id
-- WHERE f.feature_key IN ('evaluaciones.mi-perfil-prevencionista','evaluaciones.dashboard-prevencionistas')
-- GROUP BY f.feature_key;
-- Esperado: mi-perfil-prevencionista → {70,72}; dashboard-prevencionistas → {9}.
