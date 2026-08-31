-- ─────────────────────────────────────────────────────────────────────────────
-- Seguimiento de reprogramaciones de un acuerdo (no de la reunión, que ya tiene
-- su propio historial en reunion_reprogramacion). Alimenta la sección
-- "Pendientes de ediciones anteriores" en el acta: un acuerdo reprogramado 2+
-- veces se resalta como señal de alerta.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS veces_reprogramado INT NOT NULL DEFAULT 0;
ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS ultimo_motivo_reprogramacion TEXT;
