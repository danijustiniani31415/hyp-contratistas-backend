-- ============================================================================
-- Proyectos · Planeamiento BIM — Feature "planeamiento-bim.portafolio"
-- Fecha: 2026-08-17
--
-- Fase 3: Dashboard de Portafolio (landing del feature, antes de elegir
-- proyecto). Backend: PlaneamientoBimPortafolioController, ya implementado con
-- [Authorize(Roles = "1,2")] a nivel de clase (ADMINISTRADOR DEL SISTEMA,
-- ADMINISTRADOR DE UDP — sin USUARIO DE UDP, decisión explícita: es vista
-- ejecutiva agregada, no hay rol "Gerencia/Dirección" propio en la tabla role).
-- Frontend: featureKey 'planeamiento-bim.portafolio' en proyectos.routes.ts
-- (roleGuard), projects-tabs.ts (tab de Proyectos) y navigation.service.ts
-- (sidebar) — los 3 con el mismo featureKey Y el mismo `roles` explícito como
-- respaldo (roleGuard es un OR: si el featureKey no está sembrado todavía,
-- `roles` es lo que efectivamente restringe el acceso).
--
-- A diferencia del seed de 'planeamiento-bim.configuracion-inicial'
-- (20260807_PlaneamientoBimFeatureSeed.sql), este NO se asigna al rol 3
-- (USUARIO DE UDP) a propósito — mismos 2 roles que exige el controlador.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Feature (module_id se referencia por nombre del módulo "Proyectos").
INSERT INTO feature (feature_key, module_id)
SELECT 'planeamiento-bim.portafolio', m.module_id
FROM module m
WHERE m.module_name = 'Proyectos'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'planeamiento-bim.portafolio');

-- 2) Acceso: solo roles 1 (ADMINISTRADOR DEL SISTEMA) y 2 (ADMINISTRADOR DE UDP)
--    — mismos roles que exige el controlador backend. NO incluye el rol 3.
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM feature f
CROSS JOIN (VALUES (1), (2)) AS r(role_id)
WHERE f.feature_key = 'planeamiento-bim.portafolio'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );
