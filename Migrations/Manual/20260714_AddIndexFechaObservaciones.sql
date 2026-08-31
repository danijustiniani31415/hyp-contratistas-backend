-- Migración manual (pgAdmin) — índice faltante en ac_observaciones.fecha.
-- Ejecutar directamente contra la BD PostgreSQL. No usar dotnet ef.
--
-- Toda carga de la lista/dashboard/stats de Observaciones filtra y ordena por
-- fecha (ORDER BY fecha DESC en la lista, rango desde/hasta en dashboard y
-- stats). Sin este índice, cada una de esas queries hace un seq scan + sort
-- completo sobre la tabla en cada request.

CREATE INDEX IF NOT EXISTS ix_ac_observaciones_fecha ON ac_observaciones (fecha DESC);
CREATE INDEX IF NOT EXISTS ix_ac_observaciones_partida_reportada ON ac_observaciones (partida_reportada);
