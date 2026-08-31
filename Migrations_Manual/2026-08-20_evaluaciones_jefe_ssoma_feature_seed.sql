-- ============================================================================
-- Evaluaciones · Evaluación anónima al Jefe SSOMA (Flujo B) — features + accesos
--
-- Sigue el mismo patrón que 2026-08-20_evaluaciones_supervisores_contratista_feature_seed.sql:
-- el module_id se reutiliza del de una feature 'evaluaciones.*' ya existente
-- ('evaluaciones.evaluar').
--
-- Accesos:
--   evaluaciones.evaluar-jefe-ssoma    → roles 70 (Coordinador SSOMA) y 72
--     (Prevencionista) — mismos roles que exige EvJefeSsomaController para
--     GET /inicio y POST (evaluación anónima, obligatoria).
--   evaluaciones.resultados-jefe-ssoma → rol 9 (Jefe SSOMA) — mismo rol que
--     exige el backend para /resultados y /pendientes.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Features
INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.evaluar-jefe-ssoma', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.evaluar-jefe-ssoma');

INSERT INTO feature (feature_key, module_id)
SELECT 'evaluaciones.resultados-jefe-ssoma', f.module_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'evaluaciones.resultados-jefe-ssoma');

-- 2) Accesos
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (70), (72)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.evaluar-jefe-ssoma'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (9)) AS r(role_id)
WHERE f.feature_key = 'evaluaciones.resultados-jefe-ssoma'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id);

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT f.feature_key, array_agg(r.role_id ORDER BY r.role_id) AS roles_con_acceso
-- FROM feature f
-- JOIN role_feature rf ON rf.feature_id = f.feature_id
-- JOIN role r ON r.role_id = rf.role_id
-- WHERE f.feature_key IN ('evaluaciones.evaluar-jefe-ssoma','evaluaciones.resultados-jefe-ssoma')
-- GROUP BY f.feature_key;
-- Esperado: evaluar-jefe-ssoma → {70,72}; resultados-jefe-ssoma → {9}.
