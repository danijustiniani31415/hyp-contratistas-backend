-- Generaliza ssoma_pet_paso de lista plana a árbol: cada paso puede tener un padre
-- (subtítulo) y un tipo (subtitulo | paso | letra | guion) que controla cómo se
-- numera/viñetea al mostrarlo. "orden" ahora es posición entre HERMANOS del mismo
-- parent_id, no global — así reordenar dentro de un subtítulo no afecta a otro.
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

ALTER TABLE ssoma_pet_paso ADD COLUMN IF NOT EXISTS parent_id INTEGER NULL REFERENCES ssoma_pet_paso(id);
ALTER TABLE ssoma_pet_paso ADD COLUMN IF NOT EXISTS tipo VARCHAR(20) NOT NULL DEFAULT 'paso';

CREATE INDEX IF NOT EXISTS ix_ssoma_pet_paso_parent_id ON ssoma_pet_paso(parent_id);

COMMIT;
