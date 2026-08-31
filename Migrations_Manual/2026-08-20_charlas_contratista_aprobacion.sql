-- 1) Charlas de contratista: agrega el workflow de estado (Enviado/Aprobado/Rechazado)
--    que hoy no existe — el contratista sube la charla y quedaba directamente sin revisión.
-- 2) Nuevo feature 'ssoma.charlas.aprobar' para separar "quién puede aprobar/rechazar
--    charlas" (staff y contratista) de "quién puede entrar al módulo de Charlas"
--    (ssoma.gestion.charlas). Se asigna a Jefe SSOMA (9), Coordinador SSOMA (70) y
--    Prevencionista (72) — mismos ids estables que Shared/Constants/Roles.cs.
-- Idempotente: se puede correr más de una vez sin duplicar filas ni columnas.
-- Ejecutar manualmente en pgAdmin.

BEGIN;

ALTER TABLE ss_charla_contratista
    ADD COLUMN IF NOT EXISTS estado varchar(20) NOT NULL DEFAULT 'Enviado',
    ADD COLUMN IF NOT EXISTS aprobado_por_id integer NULL,
    ADD COLUMN IF NOT EXISTS aprobado_en timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS motivo_rechazo text NULL;

-- Filas ya existentes (subidas antes de este cambio): se consideran aprobadas de forma
-- implícita, para no dejar historial previo colgado en "Enviado" pendiente de revisión.
UPDATE ss_charla_contratista SET estado = 'Aprobado' WHERE estado = 'Enviado' AND created_at < now();

INSERT INTO feature (feature_key, module_id)
SELECT 'ssoma.charlas.aprobar', m.module_id
FROM module m
WHERE m.module_name = 'SSOMA'
  AND NOT EXISTS (
      SELECT 1 FROM feature WHERE feature_key = 'ssoma.charlas.aprobar'
  );

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE f.feature_key = 'ssoma.charlas.aprobar'
  AND r.role_id IN (9, 70, 72) -- Jefe SSOMA, Coordinador SSOMA, Prevencionista
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );

COMMIT;
