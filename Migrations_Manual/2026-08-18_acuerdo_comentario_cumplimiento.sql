-- ─────────────────────────────────────────────────────────────────────────────
-- Comentario de cumplimiento: cómo se levantó el acuerdo. Obligatorio si el
-- acuerdo no tiene evidencia adjunta (es el único registro de qué se hizo);
-- opcional si ya hay un archivo de evidencia.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS comentario_cumplimiento TEXT;
