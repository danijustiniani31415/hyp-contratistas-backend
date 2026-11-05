-- Guías de remisión: líneas libres (sin producto del catálogo ni inventario) y marca de bien de tercero.
-- Casos reales: "paquete de ropa", devolución de un equipo, un bien personal de un trabajador.
ALTER TABLE lb_guia_remision_item ALTER COLUMN producto_id DROP NOT NULL;
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS descripcion VARCHAR(300);
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS es_bien_tercero BOOLEAN NOT NULL DEFAULT FALSE;
