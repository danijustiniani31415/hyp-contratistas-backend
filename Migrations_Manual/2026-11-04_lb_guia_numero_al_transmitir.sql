-- Guías de remisión: el correlativo SUNAT se asigna al TRANSMITIR, no al crear el borrador.
-- Un borrador no es documento fiscal y puede eliminarse; si tomara número al crearse, cada
-- borrador eliminado dejaría un hueco en la numeración.

-- PASO 1 (obligatorio antes de desplegar el código nuevo)
ALTER TABLE lb_guia_remision ALTER COLUMN numero DROP NOT NULL;
-- UNIQUE (serie, numero) se mantiene: Postgres permite varios NULL, así que varios borradores conviven.

-- PASO 2 (solo cuando se eliminaron TODAS las guías desde la pantalla, para empezar en T001-00000001)
-- Reinicia el contador únicamente si la tabla quedó vacía; si hay guías, no hace nada.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM lb_guia_remision) THEN
        ALTER SEQUENCE lb_guia_remision_correlativo RESTART WITH 1;
        RAISE NOTICE 'Correlativo reiniciado en 1';
    ELSE
        RAISE NOTICE 'Quedan guías: no se reinició el correlativo';
    END IF;
END $$;
