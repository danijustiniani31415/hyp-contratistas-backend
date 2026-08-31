-- Migración manual (pgAdmin) — agrega "quién levantó" la observación.
-- Ejecutar directamente contra la BD PostgreSQL. No usar dotnet ef.

ALTER TABLE ac_observaciones
    ADD COLUMN IF NOT EXISTS levanta_por_worker_id INTEGER REFERENCES workers (id);

CREATE INDEX IF NOT EXISTS ix_ac_observaciones_levanta_por_worker_id
    ON ac_observaciones (levanta_por_worker_id);
