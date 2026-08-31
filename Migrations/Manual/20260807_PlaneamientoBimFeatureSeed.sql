-- ============================================================================
-- Proyectos · Planeamiento BIM — Feature "planeamiento-bim.configuracion-inicial"
-- Fecha: 2026-08-07
--
-- El módulo de Planeamiento BIM (Configuración Inicial, Carga Diaria y Bloqueos)
-- ya está implementado en backend (Features/PlaneamientoBimFeature/) y frontend
-- (proyectos.routes.ts + sidebar en navigation.service.ts), pero las 3 rutas
-- comparten el mismo feature_key 'planeamiento-bim.configuracion-inicial' y
-- nunca se sembró en la tabla `feature`, por lo que no aparecía en el sidebar
-- ni era accesible para ningún rol.
--
-- Backend: [Authorize(Roles = "1,2,3")] en PlaneamientoBimConfiguracionController
--          (ADMINISTRADOR DEL SISTEMA, ADMINISTRADOR DE UDP, USUARIO DE UDP).
-- Frontend: featureKey 'planeamiento-bim.configuracion-inicial' en
--           proyectos.routes.ts (roleGuard) y navigation.service.ts (sidebar).
--
-- Se asigna a los mismos 3 roles que ya exige el controlador, para que la
-- visibilidad del sidebar coincida con la accesibilidad real de la ruta.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Feature (module_id se referencia por nombre del módulo "Proyectos").
INSERT INTO feature (feature_key, module_id)
SELECT 'planeamiento-bim.configuracion-inicial', m.module_id
FROM module m
WHERE m.module_name = 'Proyectos'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'planeamiento-bim.configuracion-inicial');

-- 2) Acceso inicial: roles 1 (ADMINISTRADOR DEL SISTEMA), 2 (ADMINISTRADOR DE UDP)
--    y 3 (USUARIO DE UDP) — mismos roles que exige el controlador backend.
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (1), (2), (3)) AS r(role_id)
WHERE f.feature_key = 'planeamiento-bim.configuracion-inicial'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );
