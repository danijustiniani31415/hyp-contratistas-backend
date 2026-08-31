-- ============================================================================
-- Planeamiento BIM — Sectores por nivel + subestructura/superestructura
-- Fecha: 2026-08-28
--
-- Diseño elegido: HIBRIDO, sin migrar datos existentes (confirmado con el
-- usuario, opcion recomendada por menor riesgo sobre produccion real).
--   - zona_nivel_id NULL      -> sector "compartido": aplica a TODOS los
--     niveles de su zona, igual que el comportamiento actual. Los sectores
--     ya existentes quedan asi, sin tocarlos.
--   - zona_nivel_id con valor -> sector exclusivo de ESE nivel. Solo lo usan
--     sectores creados de ahora en mas a traves de Configuracion.
--   zona_id se mantiene sin cambios en ambos casos (columna NOT NULL como
--   hoy) — no se toca ni se elimina.
--
-- Se descarto mover la FK a zona_nivel_id de forma obligatoria porque
-- hubiera requerido duplicar cada sector existente una vez por nivel y
-- remapear bim_registro_diario.sector_id de las filas historicas —
-- migracion de datos real sobre produccion (D1: sin entorno separado).
--
-- tipo_estructura en bim_zona_nivel: clasifica el nivel como subestructura
-- (sotanos) o superestructura (pisos). Sin valor por defecto — los niveles
-- existentes quedan NULL hasta que alguien los clasifique desde
-- Configuracion.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

BEGIN;

ALTER TABLE bim_zona_sector
    ADD COLUMN IF NOT EXISTS zona_nivel_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_bim_zona_sector_zona_nivel'
    ) THEN
        ALTER TABLE bim_zona_sector
            ADD CONSTRAINT fk_bim_zona_sector_zona_nivel
            FOREIGN KEY (zona_nivel_id) REFERENCES bim_zona_nivel(id);
    END IF;
END $$;

COMMENT ON COLUMN bim_zona_sector.zona_nivel_id IS
    'NULL = sector compartido por todos los niveles de la zona (comportamiento historico, sin migrar). Con valor = sector exclusivo de ese nivel, usado por sectores creados desde este cambio en adelante.';

ALTER TABLE bim_zona_nivel
    ADD COLUMN IF NOT EXISTS tipo_estructura text NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_bim_zona_nivel_tipo_estructura'
    ) THEN
        ALTER TABLE bim_zona_nivel
            ADD CONSTRAINT ck_bim_zona_nivel_tipo_estructura
            CHECK (tipo_estructura IS NULL OR tipo_estructura IN ('SUBESTRUCTURA', 'SUPERESTRUCTURA'));
    END IF;
END $$;

COMMENT ON COLUMN bim_zona_nivel.tipo_estructura IS
    'SUBESTRUCTURA | SUPERESTRUCTURA | NULL (sin clasificar).';

COMMIT;
