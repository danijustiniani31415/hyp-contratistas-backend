-- Conecta Pedidos -> Compras -> Almacén -> Guía de Remisión -> confirmación en mina, hoy tres
-- módulos sueltos sin relación entre sí. Idempotente: seguro correr más de una vez.
--
-- Flujo real (H&P): Pedido APROBADO sin stock -> Logística genera Orden de Compra ligada al
-- pedido -> recibe la mercadería (con su factura) en el almacén de Lima -> genera Guía de
-- Remisión (ya liga a Lima->Mina) -> cuando el material llega a mina, alguien ahí confirma
-- cantidad recibida. Si confirma menos de lo despachado, la diferencia se ajusta en el kardex
-- de destino (que hoy se acredita de una al crear la guía) y vuelve a quedar pendiente en el
-- pedido, sin que nadie tenga que crear nada a mano.

-- 1) Trazabilidad de cantidades por ítem de pedido, etapa por etapa.
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS cantidad_en_compra NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS cantidad_recibida_almacen NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS cantidad_despachada NUMERIC(12,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS cantidad_confirmada_mina NUMERIC(12,2) NOT NULL DEFAULT 0;

-- 2) Un ítem de Orden de Compra puede nacer de un ítem de Pedido puntual — no es 1 OC = 1 Pedido:
-- una misma OC puede tener varias filas del mismo producto, cada una ligada a un pedido distinto
-- (así Logística junta varios pedidos al mismo proveedor sin perder de qué pedido viene cada uno).
ALTER TABLE lb_orden_compra_item ADD COLUMN IF NOT EXISTS pedido_item_id BIGINT REFERENCES lb_pedido_item(id);
CREATE INDEX IF NOT EXISTS idx_orden_compra_item_pedido_item ON lb_orden_compra_item(pedido_item_id);

-- 3) Factura del proveedor asociada a cada recepción (una OC puede recibirse — y facturarse — en
-- varias partes, por eso va en la bitácora de recepción, no en la cabecera de la OC).
ALTER TABLE lb_orden_compra_recepcion ADD COLUMN IF NOT EXISTS factura_numero VARCHAR(30);
ALTER TABLE lb_orden_compra_recepcion ADD COLUMN IF NOT EXISTS factura_monto NUMERIC(12,2);

-- 4) Un ítem de Guía de Remisión puede despachar un ítem de Pedido puntual (incluidos varios
-- pedidos en una sola guía/viaje), y guarda cuánto se confirmó recibido en el destino.
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS pedido_item_id BIGINT REFERENCES lb_pedido_item(id);
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS cantidad_confirmada NUMERIC(12,2);
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS confirmado_en TIMESTAMPTZ;
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS confirmado_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id);
CREATE INDEX IF NOT EXISTS idx_guia_remision_item_pedido_item ON lb_guia_remision_item(pedido_item_id);

-- 5) Permiso para confirmar recepción en destino (mina) — separado de GUIA_REMISION_CREAR/ENVIAR
-- porque lo hace alguien del lado del proyecto, no necesariamente quien despachó desde Lima.
INSERT INTO lb_permiso (codigo, descripcion) VALUES
('GUIA_REMISION_CONFIRMAR', 'Confirmar en el proyecto/mina la cantidad realmente recibida de una guía de remisión')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo = 'GUIA_REMISION_CONFIRMAR'
  AND r.codigo IN ('ALMACENERO', 'RESIDENTE', 'ADMIN')
  AND NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);
