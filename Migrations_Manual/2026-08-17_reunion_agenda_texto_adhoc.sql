-- ─────────────────────────────────────────────────────────────────────────────
-- Agenda ad-hoc para reuniones puntuales (tema personalizado, no guardado como
-- recurrente): antes quedaban sin ningún punto de agenda hasta que alguien lo
-- cargara después del recordatorio. Ahora el organizador debe definir al menos
-- un punto al agendar, guardado directo en la reunión (no depende de un tema
-- del catálogo). GetAgenda la prioriza por sobre la configuración del tema.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion ADD COLUMN IF NOT EXISTS agenda_texto TEXT;
