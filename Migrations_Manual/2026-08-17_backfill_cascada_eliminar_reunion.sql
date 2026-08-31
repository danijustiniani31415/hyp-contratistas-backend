-- ─────────────────────────────────────────────────────────────────────────────
-- Backfill: Eliminar() en ActasReunionRepository marcaba reunion.state = false
-- pero no propagaba el borrado a sus participantes ni a los temas de agenda ya
-- cargados (reunion_agenda_item), dejándolos "vivos" sueltos de una reunión que
-- ya no existe para nadie. Ya corregido en código; esto limpia lo que quedó de
-- reuniones eliminadas antes del fix.
-- ─────────────────────────────────────────────────────────────────────────────

UPDATE reunion_participante rp
SET state = false, updated_date_time = now(), updated_user_id = 1
FROM reunion r
WHERE r.reunion_id = rp.reunion_id
  AND r.state = false
  AND rp.state = true;

UPDATE reunion_agenda_item a
SET state = false, updated_date_time = now(), updated_user_id = 1
FROM reunion r
WHERE r.reunion_id = a.reunion_id
  AND r.state = false
  AND a.state = true;
