-- Sección Anexos del PETS: lista simple de archivos adjuntos.
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

CREATE TABLE IF NOT EXISTS ssoma_pet_anexo (
    id            SERIAL PRIMARY KEY,
    pet_id        INTEGER NOT NULL REFERENCES ssoma_pet(id),
    nombre        VARCHAR(255) NOT NULL,
    archivo_url   TEXT NOT NULL,
    orden         INTEGER NOT NULL DEFAULT 0,
    activo        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ssoma_pet_anexo_pet_id ON ssoma_pet_anexo(pet_id);

COMMIT;
