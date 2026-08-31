-- ─────────────────────────────────────────────────────────────────────────────
-- Acuerdo informativo: solo registra información, no requiere seguimiento ni
-- acción de ningún responsable. Se excluye del dashboard personal "Mis Acuerdos".
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS es_informativo BOOLEAN NOT NULL DEFAULT false;
