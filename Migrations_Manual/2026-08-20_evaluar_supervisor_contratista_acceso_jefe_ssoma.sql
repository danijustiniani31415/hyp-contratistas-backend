-- ============================================================================
-- Evaluaciones · Supervisores de Contratista (Flujo A) — acceso opcional para
-- el Jefe SSOMA (rol 9).
--
-- Hasta ahora solo evaluaban los roles 70/72 (Coordinador/Prevencionista SSOMA);
-- el Jefe SSOMA solo veía el consolidado (evaluaciones.ver-supervisores-contratista).
-- A pedido del usuario, el rol 9 también puede entrar a evaluar (opcional, no
-- obligatorio) — ver todos los proyectos, no solo el suyo — replicando lo que
-- ya hace EvSupervisorContratistaController/Repository tras este cambio.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

INSERT INTO role_feature (role_id, feature_id)
SELECT 9, f.feature_id
FROM feature f
WHERE f.feature_key = 'evaluaciones.evaluar-supervisor-contratista'
  AND NOT EXISTS (SELECT 1 FROM role_feature rf WHERE rf.role_id = 9 AND rf.feature_id = f.feature_id);

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT array_agg(rf.role_id ORDER BY rf.role_id) AS roles_con_acceso
-- FROM feature f JOIN role_feature rf ON rf.feature_id = f.feature_id
-- WHERE f.feature_key = 'evaluaciones.evaluar-supervisor-contratista';
-- Esperado: {9,70,72}.
