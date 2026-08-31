-- Introducción, Alcance, Objetivo, Definiciones y Restricciones pasan de "árbol de
-- pasos" a un solo bloque de texto por sección (son prosa, no un procedimiento paso a
-- paso — Procedimiento y Responsabilidades siguen usando ssoma_pet_paso porque sí
-- tienen estructura real). Una fila por (pet_id, seccion); se hace upsert desde la app.
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

CREATE TABLE IF NOT EXISTS ssoma_pet_seccion_texto (
    id            SERIAL PRIMARY KEY,
    pet_id        INTEGER NOT NULL REFERENCES ssoma_pet(id),
    seccion       VARCHAR(30) NOT NULL,
    contenido     TEXT NOT NULL DEFAULT '',
    updated_at    TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ssoma_pet_seccion_texto_pet_seccion ON ssoma_pet_seccion_texto(pet_id, seccion);

COMMIT;
