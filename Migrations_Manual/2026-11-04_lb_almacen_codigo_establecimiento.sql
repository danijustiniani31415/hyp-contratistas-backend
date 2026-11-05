-- Almacenes: código del establecimiento anexo en la Ficha RUC (0000 = domicilio fiscal).
-- La guía de remisión motivo 04 (traslado entre establecimientos de la misma empresa) lo exige
-- para el punto de partida y el de llegada. Se edita desde Almacén / Kardex > Gestionar almacenes.
ALTER TABLE lb_almacen ADD COLUMN IF NOT EXISTS codigo_establecimiento_sunat VARCHAR(4) NOT NULL DEFAULT '0000';
