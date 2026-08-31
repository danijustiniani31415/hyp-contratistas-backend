-- Permite que un alias represente "este material del ERP no pertenece a SSOMA" (item_id NULL),
-- no solo "este material del ERP mapea a este ítem del catálogo". Sin esto, rechazar una línea
-- sin match no se recordaba: la próxima carga con el mismo texto volvía a caer en revisión y
-- había que rechazarla de nuevo cada vez.

ALTER TABLE ss_material_alias ALTER COLUMN item_id DROP NOT NULL;
