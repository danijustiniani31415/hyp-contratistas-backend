-- Agrega la referencia al trabajador que realiza la OPT (observador), para poder
-- mostrar/filtrar por la empresa del observador. Antes solo se guardaba el nombre
-- como texto libre (observador_nombre), sin ID -> no había forma de saber su empresa.
-- Las OPT ya creadas quedan con observador_id NULL (no se puede reconstruir un dato
-- que nunca se guardó); a partir de este cambio, toda OPT nueva lo guarda.

ALTER TABLE ssoma_opt
  ADD COLUMN IF NOT EXISTS observador_id integer NULL;
