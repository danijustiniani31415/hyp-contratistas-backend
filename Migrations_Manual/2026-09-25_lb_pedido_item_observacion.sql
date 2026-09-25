-- Observación por ítem, además de la general del pedido (Pedido.Observacion) — para notas
-- puntuales de un producto ("cualquier marca", "urgente") sin mezclarlas con la nota general.
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS observacion VARCHAR(255);
