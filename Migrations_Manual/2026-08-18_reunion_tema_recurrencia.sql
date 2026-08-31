-- ─────────────────────────────────────────────────────────────────────────────
-- Generación automática de reuniones recurrentes (ej. "Reunión de Jefatura de
-- Proyectos" cada 2 lunes). El intervalo es fijo desde un ancla de calendario
-- (fecha_ancla) — reprogramar o cancelar una ocurrencia NO mueve el ancla de
-- la serie; la siguiente ocurrencia siempre sale en su fecha teórica.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS es_recurrente BOOLEAN NOT NULL DEFAULT FALSE;
-- Distinto del soft-delete "state": permite pausar la serie sin borrar la configuración.
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS recurrencia_activa BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS intervalo_dias INT;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS fecha_ancla DATE;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS hora_inicio TIME;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS hora_fin TIME;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS lugar VARCHAR(300);
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS dias_anticipacion INT NOT NULL DEFAULT 5;
-- Puntero de calendario (fecha TEÓRICA, no la fecha real/reprogramada de la reunión generada).
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS ultima_fecha_generada DATE;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS ultima_reunion_generada_id INT REFERENCES reunion(reunion_id);
