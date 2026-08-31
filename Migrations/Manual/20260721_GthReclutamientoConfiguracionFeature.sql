-- ============================================================================
-- Gestión GTH · Reclutamiento — Feature "Configuración" (acceso dinámico)
-- Fecha: 2026-07-21
--
-- Registra la funcionalidad de Configuración de Reclutamiento (botón + endpoints
-- config/correo-destinatarios) como una feature con su feature_key propio, para
-- que el acceso sea DINÁMICO vía role_feature (cualquier rol que se le asigne
-- podrá entrar), en lugar de tener un rol hardcodeado en el código.
--
-- Backend: [RequireFeature("gestion-gth.reclutamiento.configuracion")].
-- Frontend: authService.hasFeature('gestion-gth.reclutamiento.configuracion')
--           (allowed_features, que se refresca al re-loguear/renovar token).
--
-- Por ahora se asigna SOLO a ADMINISTRADOR DEL SISTEMA (role_id = 1). Para dar
-- acceso a otro rol, basta agregar su fila en role_feature (sin tocar código).
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- 1) Feature (module_id se referencia por nombre del módulo "Gestión GTH").
INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-gth.reclutamiento.configuracion', m.module_id
FROM module m
WHERE m.module_name = 'Gestión GTH'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'gestion-gth.reclutamiento.configuracion');

-- 2) Acceso inicial: solo rol 1 (ADMINISTRADOR DEL SISTEMA).
INSERT INTO role_feature (role_id, feature_id)
SELECT 1, f.feature_id
FROM feature f
WHERE f.feature_key = 'gestion-gth.reclutamiento.configuracion'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = 1 AND rf.feature_id = f.feature_id
  );
