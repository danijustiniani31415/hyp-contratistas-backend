-- Costeo por promedio ponderado [DECIDIDO 2026-09-24]. Un INGRESO con costo conocido (compra)
-- recalcula este promedio; una SALIDA lo lee para valorizar (snapshot que queda en el propio
-- movimiento) pero nunca lo modifica. Ver AlmacenKardexService.RegistrarMovimiento.
ALTER TABLE lb_stock ADD COLUMN IF NOT EXISTS costo_promedio NUMERIC(12,4) NOT NULL DEFAULT 0;
