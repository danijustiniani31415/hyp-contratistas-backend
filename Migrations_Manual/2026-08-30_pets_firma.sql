-- Sección "Firmas": Elaborado por / Revisado por / Aprobado por, cada uno con
-- nombre, cargo, fecha y firma (imagen opcional) — tal cual la tabla al pie del
-- PETS en Word. Una fila por (pet_id, rol).
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

CREATE TABLE IF NOT EXISTS ssoma_pet_firma (
    id            SERIAL PRIMARY KEY,
    pet_id        INTEGER NOT NULL REFERENCES ssoma_pet(id),
    rol           VARCHAR(20) NOT NULL, -- elaborado | revisado | aprobado
    nombre        VARCHAR(200) NULL,
    cargo         VARCHAR(200) NULL,
    fecha         DATE NULL,
    firma_url     TEXT NULL,
    updated_at    TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ssoma_pet_firma_pet_rol ON ssoma_pet_firma(pet_id, rol);

COMMIT;
