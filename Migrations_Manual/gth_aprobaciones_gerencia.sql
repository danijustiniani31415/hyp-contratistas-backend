-- ============================================================================
-- Gestión GTH · Aprobaciones de Gerencia
--
-- Contexto: el Gerente General decidía la solicitud de personal desde una página
-- PÚBLICA a la que entraba con un token enviado por correo (sin iniciar sesión).
-- Esa página desaparece: ahora la decisión se toma dentro de la aplicación, en la
-- nueva pantalla «Aprobaciones» del módulo Gestión GTH, que además queda como el
-- historial de todo lo aprobado/rechazado.
--
-- Este script:
--   1) Agrega gth_aprobacion_gg.decidido_user_id — quién registró la decisión.
--      (Antes no se podía saber: nadie iniciaba sesión para decidir.)
--   2) Crea la feature `gestion-gth.aprobaciones` y la asigna, por defecto, a los
--      mismos roles que hoy tienen la vista del solicitante. ⚠️ REVISAR: lo normal
--      es dejarla SOLO en los roles de gerencia — ver la nota del paso 3.
--
-- La columna `token` se conserva (es NOT NULL y tiene índice único entre las
-- vigentes) y se sigue generando, pero ya NO da acceso a nada.
--
-- Idempotente: se puede correr varias veces sin duplicar nada.
-- Requiere re-login de los usuarios afectados (allowed_features se recalcula al
-- iniciar sesión).
-- ============================================================================

BEGIN;

-- 1) Traza de quién decidió. Va aparte de updated_user_id para que un update
--    posterior (p. ej. un reenvío del correo) no la pise. Queda NULL en las
--    aprobaciones anteriores a la pantalla: esas se decidieron por enlace.
ALTER TABLE gth_aprobacion_gg
    ADD COLUMN IF NOT EXISTS decidido_user_id integer;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_gag_decidido_user'
    ) THEN
        ALTER TABLE gth_aprobacion_gg
            ADD CONSTRAINT fk_gag_decidido_user
            FOREIGN KEY (decidido_user_id) REFERENCES app_user (user_id);
    END IF;
END $$;

-- 2) Nueva feature de la pantalla «Aprobaciones» (módulo Gestión GTH).
--    ⚠️ NO hardcodear el module_id: difiere por entorno. Se DERIVA del módulo de
--       una feature hermana ya existente para ser correcto en cualquier BD.
INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-gth.aprobaciones', f.module_id
FROM feature f
WHERE f.feature_key = 'gestion-gth.reclutamiento'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'gestion-gth.aprobaciones');

-- 3) Acceso inicial: los mismos roles que ya tienen «Solicitud de Personal».
--    ⚠️ REVISAR DESPUÉS: quien tenga esta feature puede aprobar/rechazar cualquier
--       solicitud. Lo esperado es dejarla solo en el/los rol(es) de gerencia y
--       quitarla del resto con:
--         DELETE FROM role_feature rf
--         USING feature f
--         WHERE rf.feature_id = f.feature_id
--           AND f.feature_key = 'gestion-gth.aprobaciones'
--           AND rf.role_id <> <id del rol de gerencia>;
INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, f_new.feature_id
FROM role_feature rf
JOIN feature f_old
  ON f_old.feature_id = rf.feature_id
 AND f_old.feature_key = 'gestion-gth.solicitud-personal'
CROSS JOIN feature f_new
WHERE f_new.feature_key = 'gestion-gth.aprobaciones'
  AND NOT EXISTS (
    SELECT 1 FROM role_feature rf2
    WHERE rf2.role_id = rf.role_id
      AND rf2.feature_id = f_new.feature_id
  );

COMMIT;
