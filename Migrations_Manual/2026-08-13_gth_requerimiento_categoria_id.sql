-- ============================================================================
-- Gestión GTH · Reclutamiento — gth_requerimiento.categoria_id
--
-- Soporte del checkbox "Puesto personalizado" del modal «Nueva solicitud de
-- personal»: el solicitante puede escribir un puesto que no está en el
-- desplegable y elegirle una categoría existente. El puesto se da de alta en el
-- catálogo `puesto` (con esa categoría como guía, igual que el resto de filas
-- de esa tabla), pero la categoría que el solicitante declaró para la vacante se
-- guarda además en el requerimiento.
--
-- ¿Por qué en el requerimiento y no solo en `puesto`?  Porque son dos ejes
-- distintos, exactamente como en `workers`: `workers.puesto_id` es presentación
-- y `workers.categoria_id` es la categoría real del trabajador, y no tienen por
-- qué coincidir con `puesto.categoria_id` (esa es solo una guía derivada de los
-- datos). El par (puesto_id, categoria_id) del requerimiento es el que copiará
-- el onboarding a `workers` si el candidato termina siendo seleccionado.
--
-- Nullable a propósito: cuando la vacante eligió el puesto del desplegable no se
-- le pregunta la categoría, así que queda null y quien contrate deberá caer a
-- `puesto.categoria_id`. Los requerimientos ya existentes quedan igual (null).
--
-- Idempotente: ADD COLUMN IF NOT EXISTS + índice IF NOT EXISTS; la FK se crea
-- solo si no está.
-- ============================================================================

BEGIN;

ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS categoria_id integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_gth_req_categoria'
          AND conrelid = 'gth_requerimiento'::regclass
    ) THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_req_categoria
            FOREIGN KEY (categoria_id) REFERENCES categoria(categoria_id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_categoria
    ON gth_requerimiento (categoria_id);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT column_name, is_nullable, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'gth_requerimiento'
--   AND column_name = 'categoria_id';
-- Esperado: categoria_id | YES | integer
