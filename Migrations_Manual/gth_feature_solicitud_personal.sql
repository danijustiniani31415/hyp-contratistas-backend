-- ============================================================================
-- Gestión GTH · Reclutamiento — Split de vistas (solicitante vs GTH)
--
-- Contexto: el módulo Reclutamiento pasa a tener DOS vistas separadas:
--   • gestion-gth.reclutamiento        → vista de GTH (bandeja de solicitudes de
--                                         contratación de toda la organización).
--                                         YA EXISTE; conserva su feature_key.
--   • gestion-gth.solicitud-personal   → vista del solicitante (jefatura/gerencia
--                                         que registra y hace seguimiento a sus
--                                         vacantes). NUEVA feature (esta migración).
--
-- ⚠️ Cambio de semántica: antes `gestion-gth.reclutamiento` era la vista del
--    solicitante; ahora es la vista de GTH. Este script da la NUEVA feature del
--    solicitante a los mismos roles que hoy tienen `gestion-gth.reclutamiento`,
--    para que nadie pierda acceso. Revisar luego los role_feature para asignar:
--      - la vista del solicitante a las jefaturas/gerencias que piden personal, y
--      - la vista de GTH (gestion-gth.reclutamiento) solo al personal de GTH.
--
-- Idempotente: se puede correr múltiples veces sin duplicar nada.
-- Requiere re-login de los usuarios afectados (allowed_features se recalcula al
-- iniciar sesión).
-- ============================================================================

BEGIN;

-- 1) Nueva feature de la vista del solicitante (módulo Gestión GTH).
--    ⚠️ NO hardcodear el module_id: el ID de "Gestión GTH" difiere por entorno
--       (dev = 14, prod = 15; en prod el 14 es "Contabilidad"). Se DERIVA del
--       módulo de la feature hermana ya existente para ser correcto en cualquier BD.
INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-gth.solicitud-personal', f.module_id
FROM feature f
WHERE f.feature_key = 'gestion-gth.reclutamiento'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'gestion-gth.solicitud-personal');

-- 2) Asignarla a los mismos roles que hoy tienen la vista de GTH
--    (gestion-gth.reclutamiento), para conservar el acceso actual.
INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, f_new.feature_id
FROM role_feature rf
JOIN feature f_old
  ON f_old.feature_id = rf.feature_id
 AND f_old.feature_key = 'gestion-gth.reclutamiento'
CROSS JOIN feature f_new
WHERE f_new.feature_key = 'gestion-gth.solicitud-personal'
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf2
    WHERE rf2.role_id = rf.role_id
      AND rf2.feature_id = f_new.feature_id
  );

COMMIT;
