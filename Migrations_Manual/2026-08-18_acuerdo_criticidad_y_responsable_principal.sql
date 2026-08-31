-- ─────────────────────────────────────────────────────────────────────────────
-- Acuerdos: criticidad (NORMAL/MEDIO/CRITICO) y responsable principal.
--   - reunion_acuerdo: se agrega criticidad, para priorizar visualmente los
--     acuerdos más urgentes.
--   - reunion_acuerdo_responsable: se agrega es_principal, para que cuando un
--     acuerdo tenga varios responsables quede claro quién queda a cargo de que
--     se cumpla (evita que la responsabilidad se diluya entre todos).
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS criticidad VARCHAR(20) NOT NULL DEFAULT 'NORMAL';

ALTER TABLE reunion_acuerdo_responsable ADD COLUMN IF NOT EXISTS es_principal BOOLEAN NOT NULL DEFAULT FALSE;
