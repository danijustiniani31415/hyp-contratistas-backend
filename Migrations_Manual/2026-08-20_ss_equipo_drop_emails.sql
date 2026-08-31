-- ss_equipo.email_admin / email_ssoma se quitan: eran texto libre pedido al crear el
-- equipo pero ningún proceso los leía (no había alerta ni envío que los consumiera) —
-- solo generaban trabajo manual redundante con los contactos ya registrados en la
-- empresa contratista. Confirmado con el usuario antes de eliminar.
-- Ejecutar manualmente en pgAdmin.

BEGIN;

ALTER TABLE ss_equipo
    DROP COLUMN IF EXISTS email_admin,
    DROP COLUMN IF EXISTS email_ssoma;

COMMIT;
