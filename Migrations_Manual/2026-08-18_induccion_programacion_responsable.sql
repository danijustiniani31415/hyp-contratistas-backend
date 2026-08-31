-- Cada fecha del calendario de inducciones (ss_induccion_programacion) también guarda quién va
-- a darla: se copia del responsable del turno de rotación al generarse, pero queda editable de
-- forma independiente para esa fecha puntual. Ejecutar en pgAdmin.

BEGIN;

ALTER TABLE ss_induccion_programacion
    ADD COLUMN IF NOT EXISTS responsable_worker_id integer NULL REFERENCES workers (id);

COMMIT;
