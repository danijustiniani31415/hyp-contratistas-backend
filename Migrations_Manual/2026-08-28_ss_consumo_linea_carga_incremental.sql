-- Soporta carga acumulativa idempotente del Kardex de materiales (Movimiento/Partida de Control).
-- Antes, cada carga se deduplicaba por hash del archivo completo, así que subir el acumulado
-- (en vez de solo el delta semanal) duplicaba todo el consumo ya cargado. Ahora se guarda la
-- identidad de origen de cada línea (guía + movimiento + partida) para poder diferenciar, entre
-- una carga y la siguiente: líneas nuevas, líneas regularizadas (cambia cantidad/precio de una
-- guía ya cargada) y líneas dadas de baja (una guía que desaparece del acumulado, ej. anulada
-- en el ERP) sin perder la clasificación de catálogo ya hecha en las líneas que no cambiaron.

ALTER TABLE ss_consumo_linea
    ADD COLUMN IF NOT EXISTS nro_guia varchar(50),
    ADD COLUMN IF NOT EXISTS movimiento varchar(20),
    ADD COLUMN IF NOT EXISTS partida_control varchar(150),
    ADD COLUMN IF NOT EXISTS ocurrencia int NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS activo boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS motivo_inactivo varchar(200),
    ADD COLUMN IF NOT EXISTS actualizado_en timestamptz;

-- Llave natural para matchear una línea del acumulado nuevo contra lo ya guardado. Parcial
-- (solo sobre activas): una guía dada de baja puede reaparecer más adelante como una nueva
-- ocurrencia sin chocar contra el registro inactivo. nro_guia queda NULL en el consumo histórico
-- cargado antes de este cambio — Postgres permite múltiples NULL en un índice único, así que
-- esas filas no participan del match (no chocan entre sí ni con las nuevas).
CREATE UNIQUE INDEX IF NOT EXISTS ux_consumo_linea_clave_natural
    ON ss_consumo_linea (project_id, nro_guia, recurso_crudo, fecha_guia, movimiento, ocurrencia)
    WHERE activo = true;

CREATE INDEX IF NOT EXISTS ix_consumo_linea_project_activo
    ON ss_consumo_linea (project_id, activo);

-- Contadores de la última carga acumulada, para mostrar en el historial qué cambió en cada subida.
ALTER TABLE ss_consumo_carga
    ADD COLUMN IF NOT EXISTS lineas_nuevas int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS lineas_actualizadas int NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS lineas_eliminadas int NOT NULL DEFAULT 0;
