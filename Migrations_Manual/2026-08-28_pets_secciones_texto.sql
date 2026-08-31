-- Generaliza ssoma_pet_paso para reusar el mismo árbol (subtitulo/paso/letra/guion)
-- en las secciones de texto libre del PETS, no solo en "Procedimiento": Introducción,
-- Alcance, Objetivo, Definiciones, Responsabilidades, Restricciones.
-- Cada sección es un árbol independiente dentro del mismo pet_id — "hermanos" ahora
-- se agrupan por (pet_id, seccion, parent_id), no solo por (pet_id, parent_id).
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

ALTER TABLE ssoma_pet_paso ADD COLUMN IF NOT EXISTS seccion VARCHAR(30) NOT NULL DEFAULT 'procedimiento';

CREATE INDEX IF NOT EXISTS ix_ssoma_pet_paso_pet_seccion ON ssoma_pet_paso(pet_id, seccion);

COMMIT;
