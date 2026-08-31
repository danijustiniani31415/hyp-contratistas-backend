-- ============================================================================
-- Control de Licencias: renombra el feature_key para que el sidebar/roleGuard
-- sigan reconociendo el permiso ya asignado a los roles (Administrador de
-- Residentes, etc.) — sin este UPDATE, el ítem queda invisible porque el
-- código ahora pide 'vecinos.control-licencias' y la tabla feature todavía
-- tiene 'vecinos.control-vencimientos'.
-- Ejecutar manualmente en pgAdmin.
-- ============================================================================

UPDATE feature
SET feature_key = 'vecinos.control-licencias'
WHERE feature_key = 'vecinos.control-vencimientos';

-- Verificación: debe devolver 1 fila con el nuevo key.
SELECT feature_id, feature_key FROM feature WHERE feature_key = 'vecinos.control-licencias';
