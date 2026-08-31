-- ─────────────────────────────────────────────────────────────────────────────
-- Coautores del acta: un participante marcado como coautor puede editar el acta
-- igual que su creador (Update/Reprogramar/CambiarEstado/Eliminar y CRUD de
-- acuerdos). Pensado para cuando el creador se enferma o no puede asistir.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_participante ADD COLUMN IF NOT EXISTS es_coautor BOOLEAN NOT NULL DEFAULT false;
