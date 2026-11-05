-- Productos: marca como dato propio. Cada marca es un producto distinto (con su propio stock): el nombre ya
-- la incluye y este campo permite reconocerla al importar desde Excel y filtrar por marca.
ALTER TABLE lb_producto ADD COLUMN IF NOT EXISTS marca VARCHAR(60);
