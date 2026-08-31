-- ─────────────────────────────────────────────────────────────────────────────
-- Actas de Reunión debe ser visible para todos (cualquier convocado a una
-- reunión necesita entrar a cargar su agenda o ver el acta), no solo para
-- los roles que ya la tenían asignada. Se concede la feature a TODOS los
-- roles activos internos; el listado ya filtra por reunión/convocatoria vía
-- lógica de negocio (cada quien solo actúa sobre lo suyo), así que dar acceso
-- al módulo no expone nada que no debiera verse.
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN role r
WHERE f.feature_key = 'projects.actas-reunion'
  AND r.state
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf
    WHERE rf.feature_id = f.feature_id AND rf.role_id = r.role_id
  );

-- Verificación: roles que quedaron con acceso.
-- SELECT ro.role_id, ro.role_description
-- FROM role_feature rf
-- JOIN feature f ON f.feature_id = rf.feature_id
-- JOIN role ro ON ro.role_id = rf.role_id
-- WHERE f.feature_key = 'projects.actas-reunion'
-- ORDER BY ro.role_id;
