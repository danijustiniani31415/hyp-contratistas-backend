-- Regla de negocio: un usuario de contratista pertenece a UNA sola empresa
-- (vínculo vigente en contractor_user). contractor_email queda únicamente como
-- correos de contacto y sí puede repetirse entre contratistas; su columna
-- user_id ya no se escribe desde el backend (se conserva por auditoría).
--
-- Antes de crear el índice en producción hay que resolver los usuarios con
-- más de un vínculo vigente (ver limpieza en el chat de la sesión 2026-07-20):
--   SELECT user_id, COUNT(*) FROM contractor_user WHERE state
--   GROUP BY user_id HAVING COUNT(DISTINCT contractor_id) > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_contractor_user_vigente
    ON contractor_user (user_id) WHERE (state = true);
