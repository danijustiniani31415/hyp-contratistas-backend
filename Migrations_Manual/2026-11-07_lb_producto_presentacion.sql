-- Presentación de compra: el producto viene en PAQUETE/CAJA/DOCENA en la factura pero el stock se lleva en la unidad que entrega el almacenero.
ALTER TABLE lb_producto ADD COLUMN IF NOT EXISTS presentacion varchar(30) NULL;
ALTER TABLE lb_producto ADD COLUMN IF NOT EXISTS unidades_por_presentacion numeric(12,3) NULL;
