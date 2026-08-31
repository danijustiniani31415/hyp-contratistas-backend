-- Fase 2b de Planeamiento BIM: Procura simplificado como categoría de
-- bim_evidencia_foto (en vez de tabla nueva) — reutiliza POST/GET de evidencias
-- y storage ya existentes de Carga Diaria. Idempotente: seguro de re-ejecutar.

ALTER TABLE bim_evidencia_foto
    ADD COLUMN IF NOT EXISTS categoria text NOT NULL DEFAULT 'GENERAL';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'bim_evidencia_foto_categoria_check'
    ) THEN
        ALTER TABLE bim_evidencia_foto
            ADD CONSTRAINT bim_evidencia_foto_categoria_check
            CHECK (categoria = ANY (ARRAY['GENERAL', 'PROCURA']));
    END IF;
END $$;
