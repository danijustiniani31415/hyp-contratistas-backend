-- ============================================================================
-- Configuración → Categorías y Puestos
--
-- Las categorías y puestos del catálogo de trabajadores se gestionaban desde un
-- modal ("Catálogos") en la barra de acciones de Gestión de Ingresos →
-- Trabajadores. Al ser datos maestros de toda la organización pasan a ser una
-- sección propia de configuración global: /configuracion/categorias-puestos.
--
-- Este script solo registra la feature y sus roles: no toca las tablas
-- `categoria` ni `puesto` (los datos y los endpoints son los mismos de antes).
--
-- Roles habilitados (los mismos que podían abrir el viejo modal):
--   1 → ADMINISTRADOR DEL SISTEMA
--   2 → ADMINISTRADOR DE UDP
--   9 → JEFE SSOMA
--
-- Idempotente: se puede correr más de una vez sin duplicar filas. El feature_id
-- lo asigna la secuencia (la app resuelve por feature_key, no por id).
-- ============================================================================

BEGIN;

-- 1) La feature dentro del módulo "Configuración" (module_id = 2).
INSERT INTO feature (feature_key, module_id)
SELECT 'configuracion.categorias-puestos', m.module_id
FROM module m
WHERE m.module_name = 'Configuración'
  AND NOT EXISTS (
    SELECT 1 FROM feature WHERE feature_key = 'configuracion.categorias-puestos'
  );

-- 2) Roles con acceso a la nueva sección.
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (1), (2), (9)) AS r(role_id)
WHERE f.feature_key = 'configuracion.categorias-puestos'
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf
    WHERE rf.feature_id = f.feature_id AND rf.role_id = r.role_id
  );

COMMIT;

-- Verificación
-- SELECT f.feature_id, f.feature_key, f.module_id, rf.role_id, ro.role_description
-- FROM feature f
-- LEFT JOIN role_feature rf ON rf.feature_id = f.feature_id
-- LEFT JOIN role ro ON ro.role_id = rf.role_id
-- WHERE f.feature_key = 'configuracion.categorias-puestos'
-- ORDER BY rf.role_id;
