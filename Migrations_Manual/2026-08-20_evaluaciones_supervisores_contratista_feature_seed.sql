-- ============================================================================
-- Evaluaciones · Supervisores de Contratista (Flujo A) — features + accesos
--
-- Sigue el mismo patrón que 20260807_PlaneamientoBimFeatureSeed.sql: el
-- module_id se reutiliza del de una feature 'evaluaciones.*' ya existente
-- ('evaluaciones.evaluar'), así no depende de conocer el module_name exacto.
--
-- Accesos:
--   evaluaciones.evaluar-supervisor-contratista → roles 70 (Coordinador SSOMA)
--     y 72 (Prevencionista) — mismos roles que exige
--     EvSupervisorContratistaController en el backend.
--   evaluaciones.ver-supervisores-contratista   → rol 9 (Jefe SSOMA) —
--     mismo rol que exige el backend para /ver y /dashboard.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Features
INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.evaluar-supervisor-contratista', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.evaluar-supervisor-contratista');

INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.ver-supervisores-contratista', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.ver-supervisores-contratista');

-- 2) Accesos
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (70), (72)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.evaluar-supervisor-contratista'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (9)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.ver-supervisores-contratista'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT f.feature_key, array_agg(r.role_id ORDER BY r.role_id) AS roles_con_acceso
-- FROM feature f
-- JOIN role_feature rf ON rf.feature_id = f.feature_id
-- JOIN role r ON r.role_id = rf.role_id
-- WHERE f.feature_key IN ('evaluaciones.evaluar-supervisor-contratista','evaluaciones.ver-supervisores-contratista')
-- GROUP BY f.feature_key;
-- Esperado: evaluar-supervisor-contratista → {70,72}; ver-supervisores-contratista → {9}.
