-- ============================================================================
-- Planeamiento BIM — Restricciones: ubicación afectada + fecha prevista
-- Fecha: 2026-08-28
--
-- Puntos 2/3/9: bim_bloqueo (clase C# renombrada a BimRestriccion, tabla
-- física SIN renombrar) no tenía forma de indicar qué zona/nivel/sector/
-- actividad afecta, ni una fecha estimada de levantamiento (solo la real,
-- vía fecha_cierre, que ya existía).
--
-- Las 4 columnas de ubicación son nullable, sin backfill: las restricciones
-- existentes quedan sin ubicación asignada (NULL en las 4) hasta que
-- alguien las edite desde la app. No hay forma de inferir la ubicación de
-- una restricción vieja a partir de su descripción en texto libre, así que
-- no se intenta backfill.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

BEGIN;

ALTER TABLE bim_bloqueo
    ADD COLUMN IF NOT EXISTS fecha_levantamiento_prevista date NULL,
    ADD COLUMN IF NOT EXISTS zona_id integer NULL,
    ADD COLUMN IF NOT EXISTS zona_nivel_id integer NULL,
    ADD COLUMN IF NOT EXISTS zona_sector_id integer NULL,
    ADD COLUMN IF NOT EXISTS actividad_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bim_bloqueo_zona') THEN
        ALTER TABLE bim_bloqueo ADD CONSTRAINT fk_bim_bloqueo_zona
            FOREIGN KEY (zona_id) REFERENCES bim_proyecto_zona(id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bim_bloqueo_zona_nivel') THEN
        ALTER TABLE bim_bloqueo ADD CONSTRAINT fk_bim_bloqueo_zona_nivel
            FOREIGN KEY (zona_nivel_id) REFERENCES bim_zona_nivel(id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bim_bloqueo_zona_sector') THEN
        ALTER TABLE bim_bloqueo ADD CONSTRAINT fk_bim_bloqueo_zona_sector
            FOREIGN KEY (zona_sector_id) REFERENCES bim_zona_sector(id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bim_bloqueo_actividad') THEN
        ALTER TABLE bim_bloqueo ADD CONSTRAINT fk_bim_bloqueo_actividad
            FOREIGN KEY (actividad_id) REFERENCES bim_actividad(id);
    END IF;
END $$;

COMMENT ON COLUMN bim_bloqueo.fecha_levantamiento_prevista IS
    'Fecha estimada/objetivo de levantamiento de la restriccion. La fecha real ya existia como fecha_cierre.';
COMMENT ON COLUMN bim_bloqueo.zona_id IS 'Zona afectada por la restriccion, opcional.';
COMMENT ON COLUMN bim_bloqueo.zona_nivel_id IS 'Nivel afectado por la restriccion, opcional.';
COMMENT ON COLUMN bim_bloqueo.zona_sector_id IS 'Sector afectado por la restriccion, opcional.';
COMMENT ON COLUMN bim_bloqueo.actividad_id IS 'Tipo de actividad (catalogo global bim_actividad) afectado por la restriccion, opcional.';

COMMIT;
