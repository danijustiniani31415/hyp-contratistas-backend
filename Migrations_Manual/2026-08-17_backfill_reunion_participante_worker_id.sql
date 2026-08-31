-- ─────────────────────────────────────────────────────────────────────────────
-- Backfill: repara reunion_participante.worker_id perdido por un bug en el
-- frontend (reunion-detail.ts hardcodeaba workerId: null al cargar el detalle
-- para editar, y el Update() del backend sobrescribía el worker_id real con
-- ese null en cada guardado). Ya corregido en código; esto repara los datos
-- ya dañados.
--
-- Solo repara automáticamente cuando el nombre del participante calza con
-- EXACTAMENTE UN worker en estado ACTIVO — el caso ambiguo (0 o varios
-- candidatos) se deja intacto para revisión manual (ver consulta de abajo).
-- ─────────────────────────────────────────────────────────────────────────────

WITH huerfanos AS (
  SELECT rp.reunion_participante_id, rp.nombre
  FROM reunion_participante rp
  JOIN reunion r ON r.reunion_id = rp.reunion_id AND r.state
  WHERE rp.state AND rp.worker_id IS NULL
)
UPDATE reunion_participante rp
SET worker_id = cu.worker_id_candidato,
    updated_date_time = now(),
    updated_user_id = 1
FROM (
  SELECT h.reunion_participante_id, min(w.id) AS worker_id_candidato
  FROM huerfanos h
  JOIN person p ON lower(trim(p.full_name)) = lower(trim(h.nombre))
  JOIN workers w ON w.person_id = p.person_id AND w.estado = 'ACTIVO'
  GROUP BY h.reunion_participante_id
  HAVING count(*) = 1
) cu
WHERE rp.reunion_participante_id = cu.reunion_participante_id;

-- ── Revisión manual: lo que quedó sin reparar (0 o varios candidatos activos) ──
-- SELECT rp.reunion_participante_id, rp.reunion_id, r.numero, r.tema, rp.nombre
-- FROM reunion_participante rp
-- JOIN reunion r ON r.reunion_id = rp.reunion_id AND r.state
-- WHERE rp.state AND rp.worker_id IS NULL;
