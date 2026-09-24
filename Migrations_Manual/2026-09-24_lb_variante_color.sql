-- Segunda variante de producto (Color), mismo patrón que Talla [DECIDIDO 2026-09-24]: un producto
-- sigue siendo un solo maestro (Producto.RequiereColor dice si aplica), y el color real se
-- registra junto a la talla en lb_stock/lb_movimiento/lb_pedido_item/lb_orden_compra_item/
-- lb_guia_remision_item — no se duplica el producto por color. A diferencia de Talla (que tiene
-- 3 catálogos distintos: ROPA/CALZADO/GUANTES), Color es una sola lista plana, así que va en
-- lb_catalogo_valor (genérico, editable desde el front) en vez de una tabla dedicada tipo lb_talla.
-- Idempotente: seguro correr más de una vez.

ALTER TABLE lb_producto ADD COLUMN IF NOT EXISTS requiere_color BOOLEAN NOT NULL DEFAULT false;

INSERT INTO lb_catalogo_valor (tipo, valor, orden, activo)
SELECT 'COLOR', v.valor, v.orden, true
FROM (VALUES
    ('Rojo', 1), ('Naranja', 2), ('Amarillo', 3), ('Verde', 4),
    ('Azul', 5), ('Negro', 6), ('Blanco', 7), ('Plomo', 8)
) AS v(valor, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM lb_catalogo_valor c WHERE c.tipo = 'COLOR' AND c.valor = v.valor
);

ALTER TABLE lb_stock ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';
ALTER TABLE lb_movimiento ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';
ALTER TABLE lb_pedido_item ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';
ALTER TABLE lb_orden_compra_item ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';

-- El UNIQUE de stock pasa a incluir color — sin esto, "Chaleco talla L naranja" y "Chaleco talla L
-- verde" pisarían la misma fila de stock.
ALTER TABLE lb_stock DROP CONSTRAINT IF EXISTS lb_stock_almacen_id_producto_id_talla_key;
ALTER TABLE lb_stock DROP CONSTRAINT IF EXISTS lb_stock_almacen_id_producto_id_talla_color_key;
ALTER TABLE lb_stock ADD CONSTRAINT lb_stock_almacen_id_producto_id_talla_color_key
    UNIQUE (almacen_id, producto_id, talla, color);
