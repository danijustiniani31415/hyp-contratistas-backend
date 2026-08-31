-- Limpia participantes duplicados creados antes del fix de UnirseAsync
-- (el creador quedaba sin worker_id y se re-insertaba al "unirse" a su propia inspección).
-- Conserva el registro más antiguo por (inspeccion_id, nombre) y borra los repetidos.

DELETE FROM ssoma_inspeccion_participante p
USING ssoma_inspeccion_participante p2
WHERE p.inspeccion_id = p2.inspeccion_id
  AND lower(trim(p.nombre)) = lower(trim(p2.nombre))
  AND p.id > p2.id;
