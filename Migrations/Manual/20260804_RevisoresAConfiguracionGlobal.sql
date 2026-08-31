-- ============================================================================
-- Revisores de Trabajadores / Revisores de Áreas → Configuración global
--
-- Contexto: ambas pantallas definen los jefes/superiores directos de cada
-- trabajador y de cada área, así que dejan de ser configuración exclusiva de
-- Solicitud de Salidas y pasan al módulo de Configuración global:
--   • /gestion-administrativa/configuracion/revisor-salidas
--        → /configuracion/revisor-salidas
--   • /gestion-administrativa/configuracion/revisores-areas
--        → /configuracion/revisores-areas
--
-- Se hace con UPDATE (no INSERT + DELETE) para CONSERVAR el feature_id: así las
-- filas de role_feature siguen intactas y los roles con acceso hoy
-- (ADMINISTRADOR DE SOLICITUD DE SALIDAS y USUARIO DE GTH en prod) quedan
-- exactamente iguales, sin tener que copiarlas.
--
-- El module_id se DERIVA del módulo de `configuracion.proyectos` en vez de
-- hardcodearse, por si el ID del módulo "Configuración" difiere por entorno.
--
-- Idempotente: en una segunda corrida las claves antiguas ya no existen y no
-- actualiza nada.
-- Requiere re-login de los usuarios afectados (allowed_features se recalcula al
-- iniciar sesión).
-- ============================================================================

BEGIN;

UPDATE feature
SET feature_key = 'configuracion.revisor-salidas',
    module_id   = (SELECT module_id FROM feature WHERE feature_key = 'configuracion.proyectos')
WHERE feature_key = 'gestion-administrativa.config.revisor-salidas';

UPDATE feature
SET feature_key = 'configuracion.revisores-areas',
    module_id   = (SELECT module_id FROM feature WHERE feature_key = 'configuracion.proyectos')
WHERE feature_key = 'gestion-administrativa.config.revisores-areas';

COMMIT;

-- Verificación: deben salir las 2 features en el módulo "Configuración" con los
-- mismos roles que tenían antes del cambio.
-- SELECT f.feature_id, f.feature_key, m.module_name, r.role_id, r.role_description
-- FROM feature f
-- JOIN module m ON m.module_id = f.module_id
-- LEFT JOIN role_feature rf ON rf.feature_id = f.feature_id
-- LEFT JOIN role r ON r.role_id = rf.role_id
-- WHERE f.feature_key IN ('configuracion.revisor-salidas', 'configuracion.revisores-areas')
-- ORDER BY f.feature_key, r.role_id;
